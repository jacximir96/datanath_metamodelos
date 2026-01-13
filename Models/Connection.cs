using System.Text.Json.Serialization;

namespace DataNath.ApiMetadatos.Models;

public class Connection
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("clientConfigId")]
    public string? ClientConfigId { get; set; }

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("clientName")]
    public string ClientName { get; set; } = string.Empty;

    [JsonPropertyName("servidor")]
    public string Servidor { get; set; } = string.Empty;

    [JsonPropertyName("puerto")]
    public string Puerto { get; set; } = string.Empty;

    [JsonPropertyName("user")]
    public string User { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("repository")]
    public string Repository { get; set; } = string.Empty;

    [JsonPropertyName("adapter")]
    public string Adapter { get; set; } = string.Empty;

    [JsonPropertyName("associatedStores")]
    public List<string>? AssociatedStores { get; set; }

    [JsonPropertyName("storeFilterField")]
    public string? StoreFilterField { get; set; }
}
