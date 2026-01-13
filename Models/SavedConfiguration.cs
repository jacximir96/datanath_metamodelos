using System.Text.Json;
using System.Text.Json.Serialization;

namespace DataNath.ApiMetadatos.Models;

public class SavedConfiguration
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("config")]
    [JsonConverter(typeof(JsonToStringConverter))]
    public string Config { get; set; } = string.Empty;

    [JsonPropertyName("scenarios")]
    public List<Scenario>? Scenarios { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("lastUsed")]
    public DateTime? LastUsed { get; set; }
}

public class Scenario
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("isReadOnly")]
    public bool IsReadOnly { get; set; }

    [JsonPropertyName("assignments")]
    public List<List<string>> Assignments { get; set; } = new();

    [JsonPropertyName("storeFilter")]
    public string? StoreFilter { get; set; }
}
