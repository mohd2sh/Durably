namespace Durably;

internal static class WwwRootPathResolver
{
    public static string Resolve()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "wwwroot"),
            Path.Combine(Path.GetDirectoryName(typeof(DurablyUIServiceCollectionExtensions).Assembly.Location) ?? AppContext.BaseDirectory, "wwwroot")
        };

        return candidates.FirstOrDefault(Directory.Exists)
            ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
    }
}
