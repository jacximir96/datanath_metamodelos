using Newtonsoft.Json;

namespace DataNath.ApiMetadatos.Models;

public class User
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("Password")]
    public string Password { get; set; } = string.Empty;
}
