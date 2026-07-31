namespace Durably.Serialization;
/// <summary>Serializes the typed flow context to and from the string stored in the execution record.</summary>
public interface IStateSerializer
{
    string Serialize(object value);

    object? Deserialize(string json, Type type);
}
