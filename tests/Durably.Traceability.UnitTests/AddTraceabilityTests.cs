using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Durably.Traceability.UnitTests;

public sealed class AddTraceabilityTests
{
    [Fact]
    public void AddTraceability_with_UseInMemoryStore_registers_channel_sink_and_hosted_writer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDurably().UseInMemoryStore().AddTraceability();

        // Act
        using var provider = services.BuildServiceProvider();
        var sink = provider.GetRequiredService<ITraceSink>();
        var options = provider.GetRequiredService<TraceabilityOptions>();
        var store = provider.GetRequiredService<ITraceStore>();
        var hosted = provider.GetServices<IHostedService>().ToList();

        // Assert
        Assert.IsType<ChannelTraceSink>(sink);
        Assert.NotNull(options);
        Assert.NotNull(store);
        Assert.Contains(hosted, s => s is TraceWriterService);
    }

    [Fact]
    public void AddTraceability_without_ITraceStore_fails_when_resolving_writer()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddDurably().AddTraceability();
        using var provider = services.BuildServiceProvider();

        // Act
        void ResolveHosted() => provider.GetRequiredService<IEnumerable<IHostedService>>().ToList();

        // Assert
        Assert.Throws<InvalidOperationException>(ResolveHosted);
    }
}
