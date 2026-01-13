namespace DataNath.ApiMetadatos.Configuration;

public class CosmosDbSettings
{
    public string Endpoint { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string DatabaseId { get; set; } = string.Empty;
    public string ContainerId { get; set; } = string.Empty;
    public string UsersContainerId { get; set; } = string.Empty;
    public string PersistentRequirementContainerId { get; set; } = string.Empty;
    public string SavedConfigurationsContainerId { get; set; } = string.Empty;
}
