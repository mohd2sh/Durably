using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Durably;

/// <summary>Registers async trace capture on an <see cref="IDurablyBuilder"/>.</summary>
public static class DurablyTraceabilityBuilderExtensions
{
    /// <summary>
    /// Enable async per-step traceability: a bounded channel sink plus a hosted background writer.
    /// Requires an <see cref="ITraceStore"/> from a persistence provider (<c>UseInMemoryStore</c>, EF, or Dapper).
    /// </summary>
    public static IDurablyBuilder AddTraceability(this IDurablyBuilder builder, Action<TraceabilityOptions>? configure = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        var options = new TraceabilityOptions();
        configure?.Invoke(options);

        builder.Services.AddSingleton(options);

        builder.Services.AddSingleton(_ =>
        {
            return Channel.CreateBounded<TraceRecord>(new BoundedChannelOptions(options.ChannelCapacity)
            {
                FullMode = options.FullMode,
                SingleReader = true,
                SingleWriter = false
            });
        });

        builder.Services.RemoveAll<ITraceSink>();
        builder.Services.AddSingleton<ChannelTraceSink>();
        builder.Services.AddSingleton<ITraceSink>(sp => sp.GetRequiredService<ChannelTraceSink>());
        builder.Services.AddHostedService<TraceWriterService>();

        return builder;
    }
}
