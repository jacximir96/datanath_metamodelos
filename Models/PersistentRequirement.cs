using Newtonsoft.Json;

namespace DataNath.ApiMetadatos.Models;

public class PersistentRequirement
{
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    [JsonProperty("data")]
    [JsonConverter(typeof(JsonToStringConverter))]
    public string Data { get; set; } = string.Empty;

    [JsonProperty("_rid")]
    public string Rid { get; set; } = string.Empty;

    [JsonProperty("_self")]
    public string Self { get; set; } = string.Empty;

    [JsonProperty("_etag")]
    public string ETag { get; set; } = string.Empty;

    [JsonProperty("_attachments")]
    public string Attachments { get; set; } = string.Empty;

    [JsonProperty("_ts")]
    public long Timestamp { get; set; }
}
