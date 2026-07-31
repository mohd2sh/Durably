using Xunit;

namespace Durably.Persistence.EntityFrameworkCore.UnitTests;

public sealed class TraceMapperTests
{
    private const string FlowName = "orders";
    private const string InstanceId = "ord-1";
    private const string StepKey = "generate";
    private const int Attempt = 1;
    private const int DurationMs = 42;
    private const string InputJson = "{\"a\":1}";
    private const string OutputJson = "{\"b\":2}";
    private const string ExceptionMessage = "boom";

    [Fact]
    public void ToEntity_and_ToRecord_round_trip_fields()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2026, 2, 1, 12, 0, 0, TimeSpan.Zero);
        var record = new TraceRecord
        {
            FlowName = FlowName,
            InstanceId = InstanceId,
            StepKey = StepKey,
            Attempt = Attempt,
            Outcome = TraceOutcome.Failed,
            InputJson = InputJson,
            OutputJson = OutputJson,
            DurationMs = DurationMs,
            ExceptionMessage = ExceptionMessage,
            Timestamp = timestamp
        };

        // Act
        var entity = TraceMapper.ToEntity(record);
        var restored = TraceMapper.ToRecord(entity);

        // Assert
        Assert.Equal(FlowName, restored.FlowName);
        Assert.Equal(InstanceId, restored.InstanceId);
        Assert.Equal(StepKey, restored.StepKey);
        Assert.Equal(Attempt, restored.Attempt);
        Assert.Equal(TraceOutcome.Failed, restored.Outcome);
        Assert.Equal(InputJson, restored.InputJson);
        Assert.Equal(OutputJson, restored.OutputJson);
        Assert.Equal(DurationMs, restored.DurationMs);
        Assert.Equal(ExceptionMessage, restored.ExceptionMessage);
        Assert.Equal(timestamp, restored.Timestamp);
    }
}
