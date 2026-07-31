using Xunit;

namespace Durably.Core.UnitTests.Engine;
public sealed class FlowRegistryTests
{
    private sealed class RegistryFlow;

    [Fact]
    public void Register_and_TryGet_round_trips_builder_registration()
    {
        // Arrange
        var registry = new FlowRegistry();
        var flow = Flow.For<RegistryFlow, OrderState>()
            .Step("noop", (_, _) => Task.CompletedTask);
        var registration = FlowRegistration<OrderState>.FromBuilder((FlowBuilder<OrderState>)flow);

        // Act
        registry.Register(registration);
        var found = registry.TryGet(flow.Name, out var resolved);

        // Assert
        Assert.True(found);
        Assert.Same(registration, resolved);
    }

    [Fact]
    public void TryGet_unknown_flow_returns_false()
    {
        // Arrange
        var registry = new FlowRegistry();
        const string unknownFlowName = "does-not-exist";

        // Act
        var found = registry.TryGet(unknownFlowName, out var resolved);

        // Assert
        Assert.False(found);
        Assert.Null(resolved);
    }

    [Fact]
    public void FromFlowType_Materialize_builds_oop_flow()
    {
        // Arrange
        var registration = FlowRegistration<OrderState>.FromFlowType(typeof(OrderFlow));

        // Act
        var builder = registration.Materialize(services: null, StepDefaults.None);

        // Assert
        Assert.Equal(typeof(OrderFlow).FullName, builder.Name);
        Assert.True(builder.Nodes.Count >= 3);
    }
}
