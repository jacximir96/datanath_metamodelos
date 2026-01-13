using DataNath.ApiMetadatos.Configuration;
using DataNath.ApiMetadatos.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataNath.ApiMetadatos.Repositories;

public class SavedConfigurationRepository : ISavedConfigurationRepository
{
    private readonly Container _container;
    private readonly ILogger<SavedConfigurationRepository> _logger;

    public SavedConfigurationRepository(
        CosmosClient cosmosClient,
        IOptions<CosmosDbSettings> settings,
        ILogger<SavedConfigurationRepository> logger)
    {
        var cosmosDbSettings = settings.Value;
        var database = cosmosClient.GetDatabase(cosmosDbSettings.DatabaseId);
        _container = database.GetContainer(cosmosDbSettings.SavedConfigurationsContainerId);
        _logger = logger;
    }

    public async Task<List<SavedConfiguration>> GetAllAsync()
    {
        try
        {
            _logger.LogInformation("Obteniendo todas las configuraciones guardadas");

            var query = _container.GetItemQueryIterator<SavedConfiguration>(
                new QueryDefinition("SELECT * FROM c ORDER BY c.createdAt DESC"));

            var results = new List<SavedConfiguration>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }

            _logger.LogInformation("Se obtuvieron {Count} configuraciones", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las configuraciones");
            throw;
        }
    }

    public async Task<SavedConfiguration?> GetByIdAsync(string id)
    {
        try
        {
            _logger.LogInformation("Obteniendo configuración por ID: {Id}", id);

            var response = await _container.ReadItemAsync<SavedConfiguration>(id, new PartitionKey(id));
            var config = response.Resource;

            _logger.LogInformation("Configuración encontrada: {Name}", config.Name);
            return config;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Configuración no encontrada con ID: {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener configuración por ID: {Id}", id);
            throw;
        }
    }

    public async Task<SavedConfiguration> CreateAsync(SavedConfiguration config)
    {
        try
        {
            _logger.LogInformation("Creando nueva configuración: {Name}", config.Name);
            _logger.LogInformation("ID del objeto: '{Id}'", config.Id);
            _logger.LogInformation("Tipo de ID: {Type}, IsNullOrEmpty: {IsEmpty}",
                config.Id?.GetType().Name ?? "null",
                string.IsNullOrEmpty(config.Id));

            // Serializar para debug
            var json = System.Text.Json.JsonSerializer.Serialize(config);
            _logger.LogInformation("JSON a enviar a Cosmos DB: {Json}", json);

            var response = await _container.CreateItemAsync(config, new PartitionKey(config.Id));

            _logger.LogInformation("Configuración creada exitosamente con ID: {Id}", config.Id);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear configuración: {Name}", config.Name);
            throw;
        }
    }

    public async Task<SavedConfiguration?> UpdateAsync(string id, SavedConfiguration config)
    {
        try
        {
            _logger.LogInformation("Actualizando configuración: {Id}", id);

            // Asegurar que el ID coincida
            config.Id = id;

            // Actualizar lastUsed
            config.LastUsed = DateTime.UtcNow;

            var response = await _container.ReplaceItemAsync(config, id, new PartitionKey(id));

            _logger.LogInformation("Configuración actualizada exitosamente: {Id}", id);
            return response.Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Configuración no encontrada para actualizar: {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar configuración: {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string id)
    {
        try
        {
            _logger.LogInformation("Eliminando configuración: {Id}", id);

            await _container.DeleteItemAsync<SavedConfiguration>(id, new PartitionKey(id));

            _logger.LogInformation("Configuración eliminada exitosamente: {Id}", id);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Configuración no encontrada para eliminar: {Id}", id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar configuración: {Id}", id);
            throw;
        }
    }

    public async Task<SavedConfiguration?> UpdateLastUsedAsync(string id)
    {
        try
        {
            _logger.LogInformation("Actualizando lastUsed para configuración: {Id}", id);

            // Obtener la configuración actual
            var config = await GetByIdAsync(id);
            if (config == null)
            {
                return null;
            }

            // Actualizar solo lastUsed
            config.LastUsed = DateTime.UtcNow;

            var response = await _container.ReplaceItemAsync(config, id, new PartitionKey(id));

            _logger.LogInformation("LastUsed actualizado exitosamente: {Id}", id);
            return response.Resource;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar lastUsed: {Id}", id);
            throw;
        }
    }
}
