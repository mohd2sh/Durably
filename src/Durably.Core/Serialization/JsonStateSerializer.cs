using System.Text.Json;

namespace Durably.Serialization;
/// <summary>Default <see cref="IStateSerializer"/> backed by <c>System.Text.Json</c>.</summary>
internal sealed class JsonStateSerializer : IStateSerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonStateSerializer(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions(JsonSerializerDefaults.General);
    }

    public string Serialize(object value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return JsonSerializer.Serialize(value, value.GetType(), _options);
    }

    public object? Deserialize(string json, Type type)
        => JsonSerializer.Deserialize(json, type, _options);
}
