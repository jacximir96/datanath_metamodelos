using DataNath.ApiMetadatos.Configuration;
using DataNath.ApiMetadatos.Models;
using DataNath.ApiMetadatos.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DataNath.ApiMetadatos.Repositories;

public class ConnectionRepository : IConnectionRepository
{
    private readonly Container _container;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<ConnectionRepository> _logger;
    private readonly IMemoryCache _cache;

    public ConnectionRepository(
        CosmosClient cosmosClient,
        IOptions<CosmosDbSettings> settings,
        IEncryptionService encryptionService,
        ILogger<ConnectionRepository> logger,
        IMemoryCache cache)
    {
        var cosmosDbSettings = settings.Value;
        var database = cosmosClient.GetDatabase(cosmosDbSettings.DatabaseId);
        _container = database.GetContainer(cosmosDbSettings.ContainerId);
        _encryptionService = encryptionService;
        _logger = logger;
        _cache = cache;
    }

    public async Task<IEnumerable<Connection>> GetAllConnectionsAsync()
    {
        const string cacheKey = "all_connections";

        try
        {
            // Intentar obtener del caché primero
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Connection>? cachedConnections) && cachedConnections != null)
            {
                _logger.LogInformation("✅ Conexiones obtenidas desde CACHÉ ({Count} items)", cachedConnections.Count());
                return cachedConnections;
            }

            _logger.LogInformation("⏱️ Obteniendo conexiones desde Cosmos DB...");
            var startTime = DateTime.UtcNow;

            var query = _container.GetItemQueryIterator<Connection>(
                new QueryDefinition("SELECT * FROM c"));

            var results = new List<Connection>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                results.AddRange(response);
            }

            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogInformation("✅ Se obtuvieron {Count} conexiones desde Cosmos DB en {ElapsedMs}ms",
                results.Count, elapsed.TotalMilliseconds);

            // Guardar en caché por 5 minutos
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
                .SetSlidingExpiration(TimeSpan.FromMinutes(2));

            _cache.Set(cacheKey, results, cacheOptions);
            _logger.LogInformation("💾 Conexiones guardadas en caché por 5 minutos");

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todas las conexiones");
            throw;
        }
    }

    public async Task<Connection?> GetConnectionByIdAsync(string id)
    {
        try
        {
            _logger.LogInformation("Obteniendo conexión por ID: {Id}", id);

            var response = await _container.ReadItemAsync<Connection>(id, new PartitionKey(id));
            var connection = response.Resource;

            _logger.LogInformation("Conexión encontrada: {ClientName}", connection.ClientName);
            return connection;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Conexión no encontrada con ID: {Id}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener conexión por ID: {Id}", id);
            throw;
        }
    }

    public async Task<Connection?> GetConnectionByClientAndRepositoryAsync(string clientName, string? repository = null, string? adapter = null)
    {
        var queryText = "SELECT * FROM c WHERE c.clientName = @clientName";
        var queryDefinition = new QueryDefinition(queryText)
            .WithParameter("@clientName", clientName);

        if (!string.IsNullOrEmpty(repository))
        {
            queryText = "SELECT * FROM c WHERE c.clientName = @clientName AND c.repository = @repository";
            queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@clientName", clientName)
                .WithParameter("@repository", repository);
        }

        if (!string.IsNullOrEmpty(adapter))
        {
            queryText = "SELECT * FROM c WHERE c.clientName = @clientName AND c.repository = @repository AND c.adapter = @adapter";
            queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@clientName", clientName)
                .WithParameter("@repository", repository)
                .WithParameter("@adapter", adapter);
        }

        var query = _container.GetItemQueryIterator<Connection>(queryDefinition);

        if (query.HasMoreResults)
        {
            var response = await query.ReadNextAsync();
            var connection = response.FirstOrDefault();
            return connection;
        }

        return null;
    }

    public async Task<bool> ExistsConnectionAsync(string clientName, string servidor, string repository, string adapter, string? excludeId = null)
    {
        var queryText = @"
            SELECT VALUE COUNT(1)
            FROM c
            WHERE c.clientName = @clientName
            AND c.servidor = @servidor
            AND c.repository = @repository
            AND c.adapter = @adapter";

        var queryDefinition = new QueryDefinition(queryText)
            .WithParameter("@clientName", clientName)
            .WithParameter("@servidor", servidor)
            .WithParameter("@repository", repository)
            .WithParameter("@adapter", adapter);

        if (!string.IsNullOrEmpty(excludeId))
        {
            queryText += " AND c.id != @excludeId";
            queryDefinition = new QueryDefinition(queryText)
                .WithParameter("@clientName", clientName)
                .WithParameter("@servidor", servidor)
                .WithParameter("@repository", repository)
                .WithParameter("@adapter", adapter)
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

    private void InvalidateCache()
    {
        const string cacheKey = "all_connections";
        _cache.Remove(cacheKey);
        _logger.LogInformation("🗑️ Caché de conexiones invalidado");
    }

    public async Task<Connection> CreateConnectionAsync(Connection connection)
    {
        try
        {
            _logger.LogInformation("Creando nueva conexión para cliente: {ClientName}", connection.ClientName);

            // Validar que no exista una conexión duplicada usando query optimizado
            // Clave única: clientName + servidor + repository + adapter
            var exists = await ExistsConnectionAsync(
                connection.ClientName,
                connection.Servidor,
                connection.Repository,
                connection.Adapter);

            if (exists)
            {
                _logger.LogWarning("Intento de crear conexión duplicada: {ClientName}/{Servidor}/{Repository}/{Adapter}",
                    connection.ClientName, connection.Servidor, connection.Repository, connection.Adapter);

                throw new InvalidOperationException(
                    $"Ya existe una conexión con clientName='{connection.ClientName}', " +
                    $"servidor='{connection.Servidor}', repository='{connection.Repository}' " +
                    $"y adapter='{connection.Adapter}'");
            }

            var response = await _container.CreateItemAsync(connection, new PartitionKey(connection.Id));
            var savedConnection = response.Resource;

            // Invalidar caché después de crear
            InvalidateCache();

            _logger.LogInformation("Conexión creada exitosamente con ID: {Id}", savedConnection.Id);
            return savedConnection;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear conexión para cliente: {ClientName}", connection.ClientName);
            throw;
        }
    }

    public async Task<Connection?> UpdateConnectionAsync(string id, Connection connection)
    {
        try
        {
            _logger.LogInformation("Actualizando conexión ID: {Id}", id);

            connection.Id = id;

            // Validar que no exista una conexión duplicada usando query optimizado
            // Excluye el registro actual (excludeId)
            var exists = await ExistsConnectionAsync(
                connection.ClientName,
                connection.Servidor,
                connection.Repository,
                connection.Adapter,
                id); // Excluir el registro que estamos actualizando

            if (exists)
            {
                _logger.LogWarning("Intento de actualizar a conexión duplicada: {ClientName}/{Servidor}/{Repository}/{Adapter}",
                    connection.ClientName, connection.Servidor, connection.Repository, connection.Adapter);

                throw new InvalidOperationException(
                    $"Ya existe una conexión con clientName='{connection.ClientName}', " +
                    $"servidor='{connection.Servidor}', repository='{connection.Repository}' " +
                    $"y adapter='{connection.Adapter}'");
            }

            var response = await _container.ReplaceItemAsync(connection, id, new PartitionKey(id));
            var updatedConnection = response.Resource;

            // Invalidar caché después de actualizar
            InvalidateCache();

            _logger.LogInformation("Conexión actualizada exitosamente: {Id}", id);
            return updatedConnection;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Conexión no encontrada para actualizar: {Id}", id);
            return null;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar conexión ID: {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteConnectionAsync(string id)
    {
        try
        {
            _logger.LogInformation("Eliminando conexión ID: {Id}", id);
            await _container.DeleteItemAsync<Connection>(id, new PartitionKey(id));

            // Invalidar caché después de eliminar
            InvalidateCache();

            _logger.LogInformation("Conexión eliminada exitosamente: {Id}", id);
            return true;
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Conexión no encontrada para eliminar: {Id}", id);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar conexión ID: {Id}", id);
            throw;
        }
    }
}
