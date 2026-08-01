using Xunit;

namespace Durably.Core.UnitTests.Execution;

public sealed class StepPathHashTests
{
    private sealed class PathHashFlow;
    private sealed class PathHashFlowV2;
    private sealed class AppendFlow;
    private sealed class ChoosePathFlow;
    private sealed class TruncateFlow;

    [Fact]
    public async Task ProcessAsync_insert_before_cursor_quarantines_definition_mismatch()
    {
        // Arrange — run generate+enrich+email, pause before finalize (CurrentStep=3)
        var harness = EngineTestHarness.Create();
        var v1 = Flow.For<PathHashFlow, OrderState>()
            .Step("generate", (s, _) => { s.Report = "g"; return Task.CompletedTask; })
            .Step("enrich", (s, _) => { s.Report += "e"; return Task.CompletedTask; })
            .Step("email", (s, _) =>
            {
                s.EmailSent = true;
                throw new InvalidOperationException("pause");
            })
            .Step("finalize", (s, _) => { s.Finalized = true; return Task.CompletedTask; });

        await harness.StartAndProcessAsync(v1, "insert-1", new OrderState());
        var mid = await harness.Store.LoadLatestAsync(v1.Name, "insert-1", CancellationToken.None);
        Assert.Equal(2, mid!.CurrentStep); // failed on email, still at email index
        Assert.NotNull(mid.StepPathHash);

        // Force cursor past email as if email had succeeded, then register reshaped flow
        mid.Status = ExecutionStatus.Running;
        mid.FailedStep = null;
        mid.ErrorMessage = null;
        mid.CurrentStep = 3;
        mid.StepPathHash = StepPathHasher.Append(mid.StepPathHash!, "email");
        var leaseUntil = DateTimeOffset.UtcNow.Add(harness.LeaseDuration);
        Assert.True(await harness.Store.TryAcquireLeaseAsync(v1.Name, mid.RunId, harness.RunnerId, leaseUntil, CancellationToken.None));
        var leased = await harness.Store.LoadAsync(v1.Name, mid.RunId, CancellationToken.None);
        mid.Version = leased!.Version;
        mid.LockedBy = leased.LockedBy;
        await harness.Store.SaveCheckpointAsync(mid, harness.RunnerId, leaseUntil, CancellationToken.None);
        await harness.Store.ReleaseLeaseAsync(v1.Name, mid.RunId, harness.RunnerId, CancellationToken.None);

        // Same flow name, insert validate between enrich and email
        var v2 = Flow.For<PathHashFlow, OrderState>()
            .Step("generate", (s, _) => { s.Report = "g"; return Task.CompletedTask; })
            .Step("enrich", (s, _) => { s.Report += "e"; return Task.CompletedTask; })
            .Step("validate", (_, _) => Task.CompletedTask)
            .Step("email", (s, _) => { s.EmailSent = true; return Task.CompletedTask; })
            .Step("finalize", (s, _) => { s.Finalized = true; return Task.CompletedTask; });
        harness.Register(v2);

        // Act
        var result = await harness.ProcessAsync(v1.Name, "insert-1");

        // Assert
        Assert.Equal(FlowStatus.Failed, result.Status);
        Assert.Equal("_definition-mismatch", result.FailedStep);
        var status = await harness.Store.LoadAsync(v1.Name, mid.RunId, CancellationToken.None);
        Assert.Equal(ExecutionStatus.Failed, status!.Status);
        Assert.Equal("_definition-mismatch", status.FailedStep);
        Assert.Contains("definition mismatch", status.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_append_after_cursor_resumes_and_completes()
    {
        // Arrange — complete first two steps under v1, then append a step on v2
        var harness = EngineTestHarness.Create();
        var failOnThird = true;
        var v1 = Flow.For<AppendFlow, OrderState>()
            .Step("a", (_, _) => Task.CompletedTask)
            .Step("b", (_, _) => Task.CompletedTask)
            .Step("c", (_, _) =>
            {
                if (failOnThird)
                {
                    failOnThird = false;
                    throw new InvalidOperationException("pause");
                }

                return Task.CompletedTask;
            });

        await harness.StartAndProcessAsync(v1, "append-1", new OrderState());
        var mid = await harness.Store.LoadLatestAsync(v1.Name, "append-1", CancellationToken.None);
        Assert.Equal(2, mid!.CurrentStep);
        Assert.NotNull(mid.StepPathHash);

        var v2 = Flow.For<AppendFlow, OrderState>()
            .Step("a", (_, _) => Task.CompletedTask)
            .Step("b", (_, _) => Task.CompletedTask)
            .Step("c", (_, _) => Task.CompletedTask)
            .Step("d", (s, _) => { s.Finalized = true; return Task.CompletedTask; });
        harness.Register(v2);

        // Act
        var result = await harness.ProcessAsync(v1.Name, "append-1");

        // Assert
        Assert.Equal(FlowStatus.Completed, result.Status);
        var done = await harness.Store.LoadAsync(v1.Name, mid.RunId, CancellationToken.None);
        Assert.Equal(ExecutionStatus.Completed, done!.Status);
        Assert.True((await harness.LoadStateAsync<OrderState>(v1.Name, "append-1")).Finalized);
        Assert.Equal(4, done.CurrentStep);
        Assert.NotNull(done.StepPathHash);
    }

    [Fact]
    public async Task ProcessAsync_legacy_null_StepPathHash_stamps_and_continues()
    {
        // Arrange
        var harness = EngineTestHarness.Create();
        var flow = Flow.For<PathHashFlowV2, OrderState>()
            .Step("a", (_, _) => Task.CompletedTask)
            .Step("b", (s, _) => { s.Finalized = true; return Task.CompletedTask; });
        harness.Register(flow);

        var runId = Guid.NewGuid().ToString("N");
        await harness.Store.CreateAsync(new ExecutionRecord
        {
            FlowName = flow.Name,
            RunId = runId,
            InstanceId = "legacy-1",
            Status = ExecutionStatus.Running,
            CurrentStep = 1,
            ContextJson = """{"Report":null,"EmailSent":false,"Finalized":false}""",
            StepPathHash = null,
            Attempts = 0,
            Version = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, CancellationToken.None);

        // Act
        var result = await harness.ProcessAsync(flow.Name, "legacy-1");

        // Assert
        Assert.Equal(FlowStatus.Completed, result.Status);
        var done = await harness.Store.LoadAsync(flow.Name, runId, CancellationToken.None);
        Assert.Equal(ExecutionStatus.Completed, done!.Status);
        Assert.Equal(StepPathHasher.ComputePrefix(new[] { "a", "b" }, 2), done.StepPathHash);
    }

    [Fact]
    public async Task ProcessAsync_CurrentStep_exceeds_definition_quarantines()
    {
        // Arrange
        var harness = EngineTestHarness.Create();
        var flow = Flow.For<TruncateFlow, OrderState>()
            .Step("only", (_, _) => Task.CompletedTask);
        harness.Register(flow);

        var runId = Guid.NewGuid().ToString("N");
        await harness.Store.CreateAsync(new ExecutionRecord
        {
            FlowName = flow.Name,
            RunId = runId,
            InstanceId = "trunc-1",
            Status = ExecutionStatus.Running,
            CurrentStep = 5,
            ContextJson = "{}",
            StepPathHash = StepPathHasher.Seed(),
            Attempts = 0,
            Version = 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        }, CancellationToken.None);

        // Act
        var result = await harness.ProcessAsync(flow.Name, "trunc-1");

        // Assert
        Assert.Equal(FlowStatus.Failed, result.Status);
        Assert.Equal("_definition-mismatch", result.FailedStep);
        var status = await harness.Store.LoadAsync(flow.Name, runId, CancellationToken.None);
        Assert.Contains("exceeds", status!.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessAsync_skipped_choose_arms_are_in_hash_chain()
    {
        // Arrange
        var harness = EngineTestHarness.Create();
        var flow = Flow.For<ChoosePathFlow, BranchState>()
            .Choose(s => s.Kind)
            .When("a", b => b.Step("arm-a", (s, _) => { s.Path = "a"; return Task.CompletedTask; }))
            .When("b", b => b.Step("arm-b", (s, _) => { s.Path = "b"; return Task.CompletedTask; }))
            .EndChoose()
            .Step("post", (_, _) => Task.CompletedTask);

        // Act
        var result = await harness.StartAndProcessAsync(flow, "choose-1", new BranchState { Kind = "a" });

        // Assert
        Assert.Equal(FlowStatus.Completed, result.Status);
        var done = await harness.Store.LoadLatestAsync(flow.Name, "choose-1", CancellationToken.None);
        Assert.Equal(StepPathHasher.ComputePrefix(new[] { "arm-a", "arm-b", "post" }, 3), done!.StepPathHash);
    }

    [Fact]
    public void FlowBuilder_duplicate_key_throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Flow.For<PathHashFlow, OrderState>()
                .Step("same", (_, _) => Task.CompletedTask)
                .Step("same", (_, _) => Task.CompletedTask));
    }

    [Fact]
    public void FlowBuilder_empty_flow_throws_on_register()
    {
        var empty = Flow.For<PathHashFlow, OrderState>();
        var harness = EngineTestHarness.Create();
        Assert.Throws<InvalidOperationException>(() => harness.Register(empty));
    }

    [Fact]
    public void StepPathHasher_seed_and_append_are_stable()
    {
        var h0 = StepPathHasher.Seed();
        var h1 = StepPathHasher.Append(h0, "a");
        var h2 = StepPathHasher.Append(h1, "b");
        Assert.Equal(h2, StepPathHasher.ComputePrefix(new[] { "a", "b" }, 2));
        Assert.Equal(64, h0.Length);
        Assert.Equal(h0, StepPathHasher.ComputePrefix(Array.Empty<string>(), 0));
    }
}
