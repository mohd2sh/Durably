using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Durably;

/// <summary>
/// Hides endpoints from OpenAPI/ApiExplorer. Uses ExcludeFromDescription on .NET 7+;
/// falls back to <see cref="ExcludeFromDescriptionAttribute"/> metadata on .NET 6.
/// </summary>
internal static class EndpointDescriptionExtensions
{
    public static TBuilder ExcludeFromApiDescription<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
#if NET7_0_OR_GREATER
        return builder.ExcludeFromDescription();
#else
        return builder.WithMetadata(new ExcludeFromDescriptionAttribute());
#endif
    }
}
