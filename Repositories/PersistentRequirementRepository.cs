using DataNath.ApiMetadatos.Configuration;
using DataNath.ApiMetadatos.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataNath.ApiMetadatos.Repositories;

public class PersistentRequirementRepository : IPersistentRequirementRepository
{
    private readonly Container _container;
    private readonly ILogger<PersistentRequirementRepository> _logger;

    public PersistentRequirementRepository(
        CosmosClient cosmosClient,
        IOptions<CosmosDbSettings> settings,
        ILogger<PersistentRequirementRepository> logger)
    {
        var cosmosDbSettings = settings.Value;
        var database = cosmosClient.GetDatabase(cosmosDbSettings.DatabaseId);
        _container = database.GetContainer(cosmosDbSettings.PersistentRequirementContainerId);
        _logger = logger;
    }

    public async Task<IEnumerable<PersistentRequirement>> GetAllPersistentRequirementsAsync()
    {
        try
        {
            _logger.LogInformation("Obteniendo todos los persistent requirements");

            var query = _container.GetItemQueryIterator<PersistentRequirement>(
                new QueryDefinition("SELECT * FROM c"));

            var results = new List<PersistentRequirement>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }

            _logger.LogInformation("Se obtuvieron {Count} persistent requirements", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los persistent requirements");
            throw;
        }
    }

    public async Task<PersistentRequirement?> GetPersistentRequirementByIdAsync(string id)
    {
        try
        {
            _logger.LogInformation("Obteniendo persistent requirement por ID: {Id}", id);

            var response = await _container.ReadItemAsync<PersistentRequirement>(id, new PartitionKey(id));

            _logger.LogInformation("Persistent requirement encontrado con ID: {Id}", id);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Persistent requirement no encontrado con ID: {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener persistent requirement por ID: {Id}", id);
            throw;
        }
    }
}
