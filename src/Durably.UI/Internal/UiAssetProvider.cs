using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace Durably;

/// <summary>
/// Serves the dashboard assets. A physical wwwroot wins when present so repo builds pick up fresh
/// Angular output. Package consumers fall back to the resources embedded at pack time.
/// </summary>
internal static class UiAssetProvider
{
    private const string ResourcePrefix = "wwwroot/";

    private static readonly Assembly AssetAssembly = typeof(UiAssetProvider).Assembly;

    private static readonly PhysicalFileProvider? PhysicalAssets = CreatePhysicalAssets();

    public static bool HasAssets { get; } = Exists("index.html");

    public static bool Exists(string relativePath)
    {
        if (!TryNormalize(relativePath, out var normalized))
        {
            return false;
        }

        if (PhysicalAssets?.GetFileInfo(normalized).Exists == true)
        {
            return true;
        }

        using var embedded = AssetAssembly.GetManifestResourceStream(ResourcePrefix + normalized);
        return embedded is not null;
    }

    public static bool TryOpen(string relativePath, out Stream content)
    {
        content = Stream.Null;
        if (!TryNormalize(relativePath, out var normalized))
        {
            return false;
        }

        var physical = PhysicalAssets?.GetFileInfo(normalized);
        if (physical?.Exists == true)
        {
            content = physical.CreateReadStream();
            return true;
        }

        var embedded = AssetAssembly.GetManifestResourceStream(ResourcePrefix + normalized);
        if (embedded is null)
        {
            return false;
        }

        content = embedded;
        return true;
    }

    private static PhysicalFileProvider? CreatePhysicalAssets()
    {
        var root = WwwRootPathResolver.Resolve();
        return Directory.Exists(root) ? new PhysicalFileProvider(root) : null;
    }

    private static bool TryNormalize(string relativePath, out string normalized)
    {
        normalized = relativePath.Replace('\\', '/').TrimStart('/');
        if (normalized.Length == 0)
        {
            return false;
        }

        // Embedded lookups are plain string matches, so traversal has to be rejected here.
        return !normalized.Split('/').Any(segment => segment == "..");
    }
}
