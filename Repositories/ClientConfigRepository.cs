using DataNath.ApiMetadatos.Configuration;
using DataNath.ApiMetadatos.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataNath.ApiMetadatos.Repositories;

public class ClientConfigRepository : IClientConfigRepository
{
    private readonly Container _container;
    private readonly ILogger<ClientConfigRepository> _logger;

    public ClientConfigRepository(
        CosmosClient cosmosClient,
        IOptions<CosmosDbSettings> settings,
        ILogger<ClientConfigRepository> logger)
    {
        var cosmosDbSettings = settings.Value;
        var database = cosmosClient.GetDatabase(cosmosDbSettings.DatabaseId);
        _container = database.GetContainer("clientconfig");
        _logger = logger;
    }

    public async Task<IEnumerable<ClientConfig>> GetAllClientConfigsAsync()
    {
        try
        {
            _logger.LogInformation("Obteniendo todas las configuraciones de cliente");

            var query = _container.GetItemQueryIterator<ClientConfig>(
                new QueryDefinition("SELECT * FROM c"));

            var results = new List<ClientConfig>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }

            _logger.LogInformation("Se obtuvieron {Count} configuraciones de cliente", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las configuraciones de cliente");
            throw;
        }
    }

    public async Task<ClientConfig?> GetClientConfigByIdAsync(string id)
    {
        try
        {
            _logger.LogInformation("Obteniendo configuración de cliente por ID: {Id}", id);

            var response = await _container.ReadItemAsync<ClientConfig>(id, new PartitionKey(id));
            var clientConfig = response.Resource;

            _logger.LogInformation("Configuración de cliente encontrada: {Name}", clientConfig.Name);
            return clientConfig;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Configuración de cliente no encontrada con ID: {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener configuración de cliente por ID: {Id}", id);
            throw;
        }
    }

    public async Task<ClientConfig?> GetClientConfigByNameAsync(string name)
    {
        try
        {
            var queryText = "SELECT * FROM c WHERE c.name = @name";
            var queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@name", name);

            var query = _container.GetItemQueryIterator<ClientConfig>(queryDefinition);

            if (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                return response.FirstOrDefault();
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener configuración de cliente por nombre: {Name}", name);
            throw;
        }
    }

    public async Task<bool> ExistsClientConfigAsync(string name, string? excludeId = null)
    {
        var queryText = "SELECT VALUE COUNT(1) FROM c WHERE c.name = @name";
        var queryDefinition = new QueryDefinition(queryText)
            .WithParameter("@name", name);

        if (!string.IsNullOrEmpty(excludeId))
        {
            queryText += " AND c.id != @excludeId";
            queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@name", name)
                .WithParameter("@excludeId", excludeId);
        }

        var query = _container.GetItemQueryIterator<int>(queryDefinition);

        if (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            return response.FirstOrDefault() > 0;
        }

        return false;
    }

    public async Task<ClientConfig> CreateClientConfigAsync(ClientConfig clientConfig)
    {
        try
        {
            _logger.LogInformation("Creando nueva configuración de cliente: {Name}", clientConfig.Name);

            var exists = await ExistsClientConfigAsync(clientConfig.Name);

            if (exists)
            {
                _logger.LogWarning("Intento de crear configuración de cliente duplicada: {Name}", clientConfig.Name);
                throw new InvalidOperationException($"Ya existe una configuración de cliente con el nombre '{clientConfig.Name}'");
            }

            clientConfig.CreatedAt = DateTime.UtcNow;
            var response = await _container.CreateItemAsync(clientConfig, new PartitionKey(clientConfig.Id));
            var savedClientConfig = response.Resource;

            _logger.LogInformation("Configuración de cliente creada exitosamente con ID: {Id}", savedClientConfig.Id);
            return savedClientConfig;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear configuración de cliente: {Name}", clientConfig.Name);
            throw;
        }
    }

    public async Task<ClientConfig?> UpdateClientConfigAsync(string id, ClientConfig clientConfig)
    {
        try
        {
            _logger.LogInformation("Actualizando configuración de cliente ID: {Id}", id);

            clientConfig.Id = id;
            clientConfig.UpdatedAt = DateTime.UtcNow;

            var exists = await ExistsClientConfigAsync(clientConfig.Name, id);

            if (exists)
            {
                _logger.LogWarning("Intento de actualizar a configuración de cliente duplicada: {Name}", clientConfig.Name);
                throw new InvalidOperationException($"Ya existe una configuración de cliente con el nombre '{clientConfig.Name}'");
            }

            var response = await _container.ReplaceItemAsync(clientConfig, id, new PartitionKey(id));
            var updatedClientConfig = response.Resource;

            _logger.LogInformation("Configuración de cliente actualizada exitosamente: {Id}", id);
            return updatedClientConfig;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Configuración de cliente no encontrada para actualizar: {Id}", id);
            return null;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar configuración de cliente ID: {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteClientConfigAsync(string id)
    {
        try
        {
            _logger.LogInformation("Eliminando configuración de cliente ID: {Id}", id);
            await _container.DeleteItemAsync<ClientConfig>(id, new PartitionKey(id));
            _logger.LogInformation("Configuración de cliente eliminada exitosamente: {Id}", id);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Configuración de cliente no encontrada para eliminar: {Id}", id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar configuración de cliente ID: {Id}", id);
            throw;
        }
    }

    public async Task<ClientConfig> EnsureClientConfigExistsAsync(string clientConfigId, string clientName, string structureType = "same")
    {
        try
        {
            // Intentar obtener por ID
            var existing = await GetClientConfigByIdAsync(clientConfigId);
            if (existing != null)
            {
                return existing;
            }

            // Si no existe, crear uno nuevo
            _logger.LogWarning("ClientConfig con ID {Id} no existe, regenerándolo automáticamente", clientConfigId);

            var newClientConfig = new ClientConfig
            {
                Id = clientConfigId,
                Name = clientName,
                Description = "Auto-regenerado",
                StructureType = structureType,
                CreatedAt = DateTime.UtcNow
            };

            var response = await _container.CreateItemAsync(newClientConfig, new PartitionKey(newClientConfig.Id));
            _logger.LogInformation("ClientConfig regenerado exitosamente: {Id}", clientConfigId);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al asegurar existencia de ClientConfig: {Id}", clientConfigId);
            throw;
        }
    }
}
