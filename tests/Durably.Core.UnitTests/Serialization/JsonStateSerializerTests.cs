using System.Text.Json;
using Xunit;

namespace Durably.Core.UnitTests.Serialization;
public sealed class JsonStateSerializerTests
{
    private sealed class SampleState
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    [Fact]
    public void Serialize_and_Deserialize_round_trip_poco()
    {
        // Arrange
        var serializer = new JsonStateSerializer();
        var original = new SampleState { Name = "order", Count = 2 };

        // Act
        var json = serializer.Serialize(original);
        var restored = (SampleState?)serializer.Deserialize(json, typeof(SampleState));

        // Assert
        Assert.NotNull(restored);
        Assert.Equal(original.Name, restored!.Name);
        Assert.Equal(original.Count, restored.Count);
    }

    [Fact]
    public void Serialize_null_throws()
    {
        // Arrange
        var serializer = new JsonStateSerializer();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => serializer.Serialize(null!));
    }

    [Fact]
    public void Deserialize_empty_object_returns_defaults()
    {
        // Arrange
        var serializer = new JsonStateSerializer();
        const string emptyObjectJson = "{}";

        // Act
        var restored = (SampleState?)serializer.Deserialize(emptyObjectJson, typeof(SampleState));

        // Assert
        Assert.NotNull(restored);
        Assert.Equal(string.Empty, restored!.Name);
        Assert.Equal(0, restored.Count);
    }

    [Fact]
    public void Custom_options_are_applied()
    {
        // Arrange
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var serializer = new JsonStateSerializer(options);
        var state = new SampleState { Name = "x", Count = 1 };

        // Act
        var json = serializer.Serialize(state);

        // Assert
        Assert.Contains("\"name\"", json, StringComparison.Ordinal);
        Assert.Contains("\"count\"", json, StringComparison.Ordinal);
    }
}
