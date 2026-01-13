using DataNath.ApiMetadatos.Models;
using DataNath.ApiMetadatos.Repositories;
using DataNath.ApiMetadatos.Services;
using HotChocolate;
using HotChocolate.Authorization;

namespace DataNath.ApiMetadatos.GraphQL;

public class Mutation
{
    public async Task<LoginResponse> Login(
        string username,
        string password,
        [Service] IJwtService jwtService)
    {
        var isValid = await jwtService.ValidateCredentialsAsync(username, password);

        if (!isValid)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "Credenciales inválidas"
            };
        }

        var token = jwtService.GenerateToken(username);

        return new LoginResponse
        {
            Success = true,
            Message = "Login exitoso",
            Token = token
        };
    }

    [Authorize]
    public async Task<Connection> CreateConnection(
        ConnectionInput input,
        [Service] IConnectionRepository repository,
        [Service] IClientConfigRepository clientConfigRepository)
    {
        // Verificar/regenerar ClientConfig si es necesario
        if (!string.IsNullOrEmpty(input.ClientConfigId))
        {
            await clientConfigRepository.EnsureClientConfigExistsAsync(
                input.ClientConfigId,
                input.ClientName);
        }

        var connection = new Connection
        {
            Id = Guid.NewGuid().ToString(),
            ClientConfigId = input.ClientConfigId,
            ClientName = input.ClientName,
            ClientId = input.ClientId ?? string.Empty,
            Servidor = input.Servidor,
            Puerto = input.Puerto,
            User = input.User,
            Password = input.Password,
            Repository = input.Repository,
            Adapter = input.Adapter,
            AssociatedStores = input.AssociatedStores,
            StoreFilterField = input.StoreFilterField
        };

        try
        {
            return await repository.CreateConnectionAsync(connection);
        }
        catch (InvalidOperationException ex)
        {
            // Re-lanzar con el mensaje original para que GraphQL lo muestre
            throw new GraphQLException(ex.Message);
        }
    }

    [Authorize]
    public async Task<Connection?> UpdateConnection(
        string id,
        ConnectionInput input,
        [Service] IConnectionRepository repository,
        [Service] IClientConfigRepository clientConfigRepository)
    {
        // Verificar/regenerar ClientConfig si es necesario
        if (!string.IsNullOrEmpty(input.ClientConfigId))
        {
            await clientConfigRepository.EnsureClientConfigExistsAsync(
                input.ClientConfigId,
                input.ClientName);
        }

        var connection = new Connection
        {
            Id = id,
            ClientConfigId = input.ClientConfigId,
            ClientName = input.ClientName,
            ClientId = input.ClientId ?? string.Empty,
            Servidor = input.Servidor,
            Puerto = input.Puerto,
            User = input.User,
            Password = input.Password,
            Repository = input.Repository,
            Adapter = input.Adapter,
            AssociatedStores = input.AssociatedStores,
            StoreFilterField = input.StoreFilterField
        };

        try
        {
            return await repository.UpdateConnectionAsync(id, connection);
        }
        catch (InvalidOperationException ex)
        {
            // Re-lanzar con el mensaje original para que GraphQL lo muestre
            throw new GraphQLException(ex.Message);
        }
    }

    [Authorize]
    public async Task<bool> DeleteConnection(
        string id,
        [Service] IConnectionRepository repository)
    {
        return await repository.DeleteConnectionAsync(id);
    }

    // ═══════════════════════════════════════════════════════════════
    // MUTATIONS DE CLIENT CONFIG
    // ═══════════════════════════════════════════════════════════════

    [Authorize]
    public async Task<ClientConfig> CreateClientConfig(
        ClientConfigInput input,
        [Service] IClientConfigRepository repository)
    {
        var clientConfig = new ClientConfig
        {
            Id = Guid.NewGuid().ToString(),
            Name = input.Name,
            Description = input.Description,
            StructureType = input.StructureType ?? "same",
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            return await repository.CreateClientConfigAsync(clientConfig);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ex.Message);
        }
    }

    [Authorize]
    public async Task<ClientConfig?> UpdateClientConfig(
        string id,
        ClientConfigInput input,
        [Service] IClientConfigRepository repository)
    {
        var clientConfig = new ClientConfig
        {
            Id = id,
            Name = input.Name,
            Description = input.Description,
            StructureType = input.StructureType ?? "same"
        };

        try
        {
            return await repository.UpdateClientConfigAsync(id, clientConfig);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(ex.Message);
        }
    }

    [Authorize]
    public async Task<bool> DeleteClientConfig(
        string id,
        [Service] IClientConfigRepository repository)
    {
        return await repository.DeleteClientConfigAsync(id);
    }

    // ═══════════════════════════════════════════════════════════════
    // MUTATIONS DE SAVED CONFIGURATIONS
    // ═══════════════════════════════════════════════════════════════

    [Authorize]
    [GraphQLName("saveSavedConfiguration")]
    public async Task<SavedConfiguration> SaveSavedConfiguration(
        SavedConfigurationInput input,
        [Service] ISavedConfigurationRepository repository)
    {
        var config = new SavedConfiguration
        {
            Id = Guid.NewGuid().ToString(), // Generar ID aquí explícitamente
            Name = input.Name,
            Description = input.Description,
            Config = input.Config,
            Scenarios = input.Scenarios?.Select(s => new Scenario
            {
                Id = s.Id,
                Name = s.Name,
                IsReadOnly = s.IsReadOnly,
                Assignments = s.Assignments,
                StoreFilter = s.StoreFilter
            }).ToList(),
            CreatedAt = DateTime.UtcNow
        };

        return await repository.CreateAsync(config);
    }

    [Authorize]
    [GraphQLName("updateSavedConfiguration")]
    public async Task<SavedConfiguration?> UpdateSavedConfiguration(
        string id,
        SavedConfigurationInput input,
        [Service] ISavedConfigurationRepository repository)
    {
        var config = new SavedConfiguration
        {
            Id = id,
            Name = input.Name,
            Description = input.Description,
            Config = input.Config,
            Scenarios = input.Scenarios?.Select(s => new Scenario
            {
                Id = s.Id,
                Name = s.Name,
                IsReadOnly = s.IsReadOnly,
                Assignments = s.Assignments,
                StoreFilter = s.StoreFilter
            }).ToList()
        };

        return await repository.UpdateAsync(id, config);
    }

    [Authorize]
    [GraphQLName("deleteSavedConfiguration")]
    public async Task<bool> DeleteSavedConfiguration(
        string id,
        [Service] ISavedConfigurationRepository repository)
    {
        return await repository.DeleteAsync(id);
    }

    [Authorize]
    [GraphQLName("updateLastUsed")]
    public async Task<SavedConfiguration?> UpdateLastUsed(
        string id,
        [Service] ISavedConfigurationRepository repository)
    {
        return await repository.UpdateLastUsedAsync(id);
    }
}

public record ConnectionInput(
    string? ClientConfigId,
    string ClientName,
    string? ClientId,
    string Servidor,
    string Puerto,
    string User,
    string Password,
    string Repository,
    string Adapter,
    List<string>? AssociatedStores = null,
    string? StoreFilterField = null
);

public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
}

public record SavedConfigurationInput(
    string Name,
    string? Description,
    string Config,
    List<ScenarioInput>? Scenarios
);

public record ScenarioInput(
    int Id,
    string Name,
    bool IsReadOnly,
    List<List<string>> Assignments,
    string? StoreFilter
);

public record ClientConfigInput(
    string Name,
    string? Description,
    string? StructureType
);
