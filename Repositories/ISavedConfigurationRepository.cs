using DataNath.ApiMetadatos.Models;

namespace DataNath.ApiMetadatos.Repositories;

public interface ISavedConfigurationRepository
{
    Task<List<SavedConfiguration>> GetAllAsync();
    Task<SavedConfiguration?> GetByIdAsync(string id);
    Task<SavedConfiguration> CreateAsync(SavedConfiguration config);
    Task<SavedConfiguration?> UpdateAsync(string id, SavedConfiguration config);
    Task<bool> DeleteAsync(string id);
    Task<SavedConfiguration?> UpdateLastUsedAsync(string id);
}
