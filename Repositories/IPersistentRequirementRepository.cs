using DataNath.ApiMetadatos.Models;

namespace DataNath.ApiMetadatos.Repositories;

public interface IPersistentRequirementRepository
{
    Task<IEnumerable<PersistentRequirement>> GetAllPersistentRequirementsAsync();
    Task<PersistentRequirement?> GetPersistentRequirementByIdAsync(string id);
}
