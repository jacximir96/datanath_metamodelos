using Newtonsoft.Json;

namespace DataNath.ApiMetadatos.Models;

public class Connection
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("clientName")]
    public string ClientName { get; set; } = string.Empty;

    [JsonProperty("servidor")]
    public string Servidor { get; set; } = string.Empty;

    [JsonProperty("puerto")]
    public string Puerto { get; set; } = string.Empty;

    [JsonProperty("user")]
    public string User { get; set; } = string.Empty;

    [JsonProperty("password")]
    public string Password { get; set; } = string.Empty;

    [JsonProperty("repository")]
    public string Repository { get; set; } = string.Empty;

    [JsonProperty("adapter")]
    public string Adapter { get; set; } = string.Empty;
}
