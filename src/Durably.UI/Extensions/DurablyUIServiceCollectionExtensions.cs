using Microsoft.Extensions.DependencyInjection;

namespace Durably;

/// <summary>Dependency injection registration for <c>Durably.UI</c>.</summary>
public static class DurablyUIServiceCollectionExtensions
{
    /// <summary>Register Durably UI options. Requires <see cref="IExecutionQuery"/> and <see cref="ITraceQuery"/> from the persistence provider.</summary>
    public static IServiceCollection AddDurablyUI(
        this IServiceCollection services,
        Action<DurablyUIOptions>? configure = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddOptions<DurablyUIOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
