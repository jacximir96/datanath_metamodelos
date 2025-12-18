using DataNath.ApiMetadatos.Models;

namespace DataNath.ApiMetadatos.Repositories;

public interface IConnectionRepository
{
    Task<IEnumerable<Connection>> GetAllConnectionsAsync();
    Task<Connection?> GetConnectionByIdAsync(string id);
    Task<Connection?> GetConnectionByClientAndRepositoryAsync(string clientName, string? repository = null, string? adapter = null);
    Task<bool> ExistsConnectionAsync(string clientName, string servidor, string repository, string adapter, string? excludeId = null);
    Task<Connection> CreateConnectionAsync(Connection connection);
    Task<Connection?> UpdateConnectionAsync(string id, Connection connection);
    Task<bool> DeleteConnectionAsync(string id);
}
