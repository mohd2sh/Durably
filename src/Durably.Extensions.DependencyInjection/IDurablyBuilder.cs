using Microsoft.Extensions.DependencyInjection;

namespace Durably;

/// <summary>
/// Returned by <c>AddDurably</c>; the seam persistence providers (<c>UseInMemoryStore</c>,
/// <c>UseSqlServer</c>, etc.) and flow registration extend. Exposes the underlying
/// <see cref="IServiceCollection"/> and options.
/// </summary>
public interface IDurablyBuilder
{
    IServiceCollection Services { get; }

    DurablyOptions Options { get; }
}

internal sealed class DurablyBuilder : IDurablyBuilder
{
    public DurablyBuilder(IServiceCollection services, DurablyOptions options)
    {
        Services = services;
        Options = options;
    }

    public IServiceCollection Services { get; }

    public DurablyOptions Options { get; }
}
