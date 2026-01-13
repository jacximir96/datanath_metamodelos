using DataNath.ApiMetadatos.Models;

namespace DataNath.ApiMetadatos.Repositories;

public interface IClientConfigRepository
{
    Task<IEnumerable<ClientConfig>> GetAllClientConfigsAsync();
    Task<ClientConfig?> GetClientConfigByIdAsync(string id);
    Task<ClientConfig?> GetClientConfigByNameAsync(string name);
    Task<bool> ExistsClientConfigAsync(string name, string? excludeId = null);
    Task<ClientConfig> CreateClientConfigAsync(ClientConfig clientConfig);
    Task<ClientConfig?> UpdateClientConfigAsync(string id, ClientConfig clientConfig);
    Task<bool> DeleteClientConfigAsync(string id);
    Task<ClientConfig> EnsureClientConfigExistsAsync(string clientConfigId, string clientName, string structureType = "same");
}
