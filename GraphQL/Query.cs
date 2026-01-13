using DataNath.ApiMetadatos.Models;
using DataNath.ApiMetadatos.Repositories;
using DataNath.ApiMetadatos.Services;
using HotChocolate.Authorization;

namespace DataNath.ApiMetadatos.GraphQL;

public class Query
{
    [Authorize]
    [GraphQLName("getConnections")]
    public async Task<ConnectionsResponse> GetConnections(
        string? clientId = null,
        string? clientName = null,
        int skip = 0,
        int take = 10,
        [Service] IConnectionRepository repository = null!)
    {
        var connections = await repository.GetAllConnectionsAsync();

        // Aplicar filtro por clientId (búsqueda exacta, case-insensitive)
        if (!string.IsNullOrEmpty(clientId))
        {
            connections = connections.Where(c => c.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase));
        }

        // Aplicar filtro por clientName (búsqueda exacta, case-insensitive)
        if (!string.IsNullOrEmpty(clientName))
        {
            connections = connections.Where(c => c.ClientName.Equals(clientName, StringComparison.OrdinalIgnoreCase));
        }

        var totalCount = connections.Count();

        // Aplicar paginación
        var paginatedConnections = connections
            .Skip(skip)
            .Take(take)
            .ToList();

        return new ConnectionsResponse
        {
            Items = paginatedConnections,
            TotalCount = totalCount,
            Skip = skip,
            Take = take
        };
    }

    [Authorize]
    [GraphQLName("getConnectionById")]
    public async Task<Connection?> GetConnectionById(
        string id,
        [Service] IConnectionRepository repository)
    {
        return await repository.GetConnectionByIdAsync(id);
    }

    // ═══════════════════════════════════════════════════════════════
    // NUEVAS QUERIES DE METADATA - Usando conexiones de Cosmos DB
    // ═══════════════════════════════════════════════════════════════

    private async Task<DatabaseConnectionInfo> GetDatabaseConnectionInfoAsync(
        string clientName,
        IConnectionRepository connectionRepo,
        string? repository = null,
        string? adapter = null)
    {
        var connection = await connectionRepo.GetConnectionByClientAndRepositoryAsync(
            clientName, repository, adapter);

        if (connection == null)
        {
            var errorMsg = $"No se encontró conexión para el cliente '{clientName}'";
            if (!string.IsNullOrEmpty(repository))
                errorMsg += $" con repository '{repository}'";
            if (!string.IsNullOrEmpty(adapter))
                errorMsg += $" y adapter '{adapter}'";
            throw new GraphQLException(errorMsg);
        }

        return new DatabaseConnectionInfo
        {
            Servidor = connection.Servidor,
            Puerto = connection.Puerto,
            User = connection.User,
            Password = connection.Password,
            Repository = connection.Repository,
            Adapter = connection.Adapter
        };
    }

    [Authorize]
    [GraphQLName("getTables")]
    public async Task<List<string>> GetTables(
        string clientName,
        [Service] IConnectionRepository connectionRepo,
        [Service] IEncryptionService encryptionService,
        [Service] DatabaseMetadataServiceFactory factory,
        string? repository = null,
        string? adapter = null)
    {
        var connectionInfo = await GetDatabaseConnectionInfoAsync(
            clientName, connectionRepo, repository, adapter);

        var metadataService = factory.CreateService(connectionInfo.Adapter);
        return await metadataService.GetTablesAsync(connectionInfo);
    }

    [Authorize]
    [GraphQLName("getTableColumns")]
    public async Task<List<ColumnInfo>> GetTableColumns(
        string clientName,
        string tableName,
        [Service] IConnectionRepository connectionRepo,
        [Service] IEncryptionService encryptionService,
        [Service] DatabaseMetadataServiceFactory factory,
        string? repository = null,
        string? adapter = null)
    {
        var connectionInfo = await GetDatabaseConnectionInfoAsync(
            clientName, connectionRepo, repository, adapter);

        var metadataService = factory.CreateService(connectionInfo.Adapter);
        return await metadataService.GetTableColumnsAsync(connectionInfo, tableName);
    }

    [Authorize]
    [GraphQLName("getTableRelations")]
    public async Task<List<RelationInfo>> GetTableRelations(
        string clientName,
        string tableName,
        [Service] IConnectionRepository connectionRepo,
        [Service] IEncryptionService encryptionService,
        [Service] DatabaseMetadataServiceFactory factory,
        string? repository = null,
        string? adapter = null)
    {
        var connectionInfo = await GetDatabaseConnectionInfoAsync(
            clientName, connectionRepo, repository, adapter);

        var metadataService = factory.CreateService(connectionInfo.Adapter);
        return await metadataService.GetTableRelationsAsync(connectionInfo, tableName);
    }

    // ═══════════════════════════════════════════════════════════════
    // QUERIES DE METADATA - Pasando credenciales directamente
    // ═══════════════════════════════════════════════════════════════

    [Authorize]
    [GraphQLName("getTablesFromConnection")]
    public async Task<List<string>> GetTablesFromConnection(
        DatabaseConnectionInfo connection,
        [Service] DatabaseMetadataServiceFactory factory)
    {
        var metadataService = factory.CreateService(connection.Adapter);
        return await metadataService.GetTablesAsync(connection);
    }

    [Authorize]
    [GraphQLName("getTableColumnsFromConnection")]
    public async Task<List<ColumnInfo>> GetTableColumnsFromConnection(
        DatabaseConnectionInfo connection,
        string tableName,
        [Service] DatabaseMetadataServiceFactory factory)
    {
        var metadataService = factory.CreateService(connection.Adapter);
        return await metadataService.GetTableColumnsAsync(connection, tableName);
    }

    [Authorize]
    [GraphQLName("getTableRelationsFromConnection")]
    public async Task<List<RelationInfo>> GetTableRelationsFromConnection(
        DatabaseConnectionInfo connection,
        string tableName,
        [Service] DatabaseMetadataServiceFactory factory)
    {
        var metadataService = factory.CreateService(connection.Adapter);
        return await metadataService.GetTableRelationsAsync(connection, tableName);
    }

    [Authorize]
    [GraphQLName("getDistinctValues")]
    public async Task<List<string>> GetDistinctValues(
        DatabaseConnectionInfo connection,
        string collectionName,
        string fieldName,
        [Service] DatabaseMetadataServiceFactory factory)
    {
        var metadataService = factory.CreateService(connection.Adapter);
        return await metadataService.GetDistinctValuesAsync(connection, collectionName, fieldName);
    }

    // ═══════════════════════════════════════════════════════════════
    // QUERIES DE PERSISTENT REQUIREMENT
    // ═══════════════════════════════════════════════════════════════

    [Authorize]
    [GraphQLName("getPersistentRequirements")]
    public async Task<PersistentRequirementsResponse> GetPersistentRequirements(
        int skip = 0,
        int take = 10,
        [Service] IPersistentRequirementRepository repository = null!)
    {
        var requirements = await repository.GetAllPersistentRequirementsAsync();

        var totalCount = requirements.Count();

        // Aplicar paginación
        var paginatedRequirements = requirements
            .Skip(skip)
            .Take(take)
            .ToList();

        return new PersistentRequirementsResponse
        {
            Items = paginatedRequirements,
            TotalCount = totalCount,
            Skip = skip,
            Take = take
        };
    }

    [Authorize]
    [GraphQLName("getPersistentRequirementById")]
    public async Task<PersistentRequirement?> GetPersistentRequirementById(
        string id,
        [Service] IPersistentRequirementRepository repository)
    {
        return await repository.GetPersistentRequirementByIdAsync(id);
    }

    // ═══════════════════════════════════════════════════════════════
    // QUERIES DE SAVED CONFIGURATIONS
    // ═══════════════════════════════════════════════════════════════

    [Authorize]
    [GraphQLName("getSavedConfigurations")]
    public async Task<List<SavedConfiguration>> GetSavedConfigurations(
        [Service] ISavedConfigurationRepository repository)
    {
        return await repository.GetAllAsync();
    }

    [Authorize]
    [GraphQLName("getSavedConfigurationById")]
    public async Task<SavedConfiguration?> GetSavedConfigurationById(
        string id,
        [Service] ISavedConfigurationRepository repository)
    {
        return await repository.GetByIdAsync(id);
    }

    // ═══════════════════════════════════════════════════════════════
    // QUERIES DE CLIENT CONFIG
    // ═══════════════════════════════════════════════════════════════

    [Authorize]
    [GraphQLName("getClientConfigs")]
    public async Task<List<ClientConfig>> GetClientConfigs(
        [Service] IClientConfigRepository repository)
    {
        var result = await repository.GetAllClientConfigsAsync();
        return result.ToList();
    }

    [Authorize]
    [GraphQLName("getClientConfigById")]
    public async Task<ClientConfig?> GetClientConfigById(
        string id,
        [Service] IClientConfigRepository repository)
    {
        return await repository.GetClientConfigByIdAsync(id);
    }

    [Authorize]
    [GraphQLName("getClientConfigByName")]
    public async Task<ClientConfig?> GetClientConfigByName(
        string name,
        [Service] IClientConfigRepository repository)
    {
        return await repository.GetClientConfigByNameAsync(name);
    }
}

public class ConnectionsResponse
{
    public List<Connection> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public int PageCount => (int)Math.Ceiling((double)TotalCount / Take);
}

public class PersistentRequirementsResponse
{
    public List<PersistentRequirement> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Take { get; set; }
    public int PageCount => (int)Math.Ceiling((double)TotalCount / Take);
}
