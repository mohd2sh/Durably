using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Durably.Extensions.DependencyInjection.UnitTests;

public sealed class FlowAssemblyScannerTests
{
    [Fact]
    public void RegisterFlows_discovers_IFlow_and_IStep_types()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        FlowAssemblyScanner.RegisterFlows(services, typeof(ScannerMarkerFlow).Assembly);

        // Assert
        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<ScannerMarkerFlow>());
        Assert.NotNull(provider.GetService<ScannerMarkerStep>());
        Assert.Contains(
            provider.GetServices<IFlowRegistration>(),
            r => r.Name == typeof(ScannerMarkerFlow).FullName);
    }

    public sealed class ScannerMarkerState;

    public sealed class ScannerMarkerFlow : IFlow<ScannerMarkerState>
    {
        public void Build(IFlowBuilder<ScannerMarkerState> builder) =>
            builder.Step<ScannerMarkerStep>();
    }

    public sealed class ScannerMarkerStep : IStep<ScannerMarkerState>
    {
        public Task ExecuteAsync(ScannerMarkerState state, IStepContext context, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}
