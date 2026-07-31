using System.Text.Json;

namespace Durably.Execution;
internal static class ExecutionMetadataSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    public static string? Serialize(IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(metadata, SerializerOptions);
    }
}
