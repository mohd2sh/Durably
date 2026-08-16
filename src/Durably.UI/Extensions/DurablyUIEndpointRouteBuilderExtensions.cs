using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Durably;

/// <summary>Endpoint mapping for the embeddable Durably observability UI.</summary>
public static class DurablyUIEndpointRouteBuilderExtensions
{
    private const string BasePlaceholder = "#uiPath#";
    private const string ApiPlaceholder = "#apiPath#";

    /// <summary>
    /// Maps the dashboard JSON API and SPA at <paramref name="routePrefix"/> (default from options).
    /// Anonymous by default. Chain <c>RequireAuthorization()</c> to harden.
    /// </summary>
    public static IEndpointConventionBuilder MapDurablyUI(
        this IEndpointRouteBuilder endpoints,
        string? routePrefix = null)
    {
        if (endpoints is null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<DurablyUIOptions>>().Value;
        var prefix = RoutePrefixNormalizer.Normalize(routePrefix ?? options.RoutePrefix);
        var apiPath = $"{prefix}/{DurablyUIRoutes.ApiRoot}";

        var apiBuilders = endpoints.MapExecutionsEndpoints(apiPath);
        var spaBuilders = MapSpa(endpoints, prefix, apiPath);
        var all = new IEndpointConventionBuilder[apiBuilders.Count + spaBuilders.Count];
        for (var i = 0; i < apiBuilders.Count; i++)
        {
            all[i] = apiBuilders[i];
        }

        for (var i = 0; i < spaBuilders.Count; i++)
        {
            all[apiBuilders.Count + i] = spaBuilders[i];
        }

        return new CompositeEndpointConventionBuilder(all);
    }

    private static IReadOnlyList<IEndpointConventionBuilder> MapSpa(
        IEndpointRouteBuilder endpoints,
        string prefix,
        string apiPath)
    {
        if (!UiAssetProvider.HasAssets)
        {
            return Array.Empty<IEndpointConventionBuilder>();
        }

        var builders = new List<IEndpointConventionBuilder>(2);

        builders.Add(
            endpoints.MapGet(prefix, () => Results.Redirect($"{prefix}/index.html"))
                .ExcludeFromApiDescription());

        builders.Add(
            endpoints.MapGet($"{prefix}/{{**spaPath}}", async context =>
            {
                var requestPath = context.Request.Path.Value ?? string.Empty;
                if (requestPath.Contains($"/{DurablyUIRoutes.ApiRoot}/", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var relativePath = requestPath[prefix.Length..].TrimStart('/');
                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    relativePath = "index.html";
                }

                if (!UiAssetProvider.TryOpen(relativePath, out var asset))
                {
                    relativePath = "index.html";
                    if (!UiAssetProvider.TryOpen(relativePath, out asset))
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }
                }

                await using (asset.ConfigureAwait(false))
                {
                    context.Response.ContentType = ResolveContentType(relativePath);

                    if (IsHtmlShell(relativePath))
                    {
                        await WriteRewrittenHtmlAsync(context, asset, prefix, apiPath).ConfigureAwait(false);
                        return;
                    }

                    await asset.CopyToAsync(context.Response.Body).ConfigureAwait(false);
                }
            })
            .ExcludeFromApiDescription());

        return builders;
    }

    private static bool IsHtmlShell(string relativePath) =>
        relativePath.Equals("index.html", StringComparison.OrdinalIgnoreCase);

    private static async Task WriteRewrittenHtmlAsync(
        HttpContext context,
        Stream asset,
        string prefix,
        string apiPath)
    {
        using var reader = new StreamReader(asset);
        var html = await reader.ReadToEndAsync().ConfigureAwait(false);
        html = html
            .Replace(BasePlaceholder, prefix, StringComparison.Ordinal)
            .Replace(ApiPlaceholder, apiPath, StringComparison.Ordinal);

        await context.Response.WriteAsync(html).ConfigureAwait(false);
    }

    private static string ResolveContentType(string relativePath)
    {
        if (relativePath.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
        {
            return "application/javascript";
        }

        if (relativePath.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
        {
            return "text/css";
        }

        if (relativePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return "application/json";
        }

        if (relativePath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
        {
            return "image/svg+xml";
        }

        return "text/html";
    }
}
