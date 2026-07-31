using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Durably;

internal sealed class CompositeEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly List<IEndpointConventionBuilder> _builders;

    public CompositeEndpointConventionBuilder(params IEndpointConventionBuilder[] builders)
    {
        _builders = builders?.Where(b => b is not null).ToList()
            ?? throw new ArgumentNullException(nameof(builders));
    }

    public void Add(Action<EndpointBuilder> convention)
    {
        foreach (var builder in _builders)
        {
            builder.Add(convention);
        }
    }
}
