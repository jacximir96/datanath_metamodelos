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
        [Service] IConnectionRepository repository)
    {
        var connection = new Connection
        {
            Id = Guid.NewGuid().ToString(),
            ClientName = input.ClientName,
            Servidor = input.Servidor,
            Puerto = input.Puerto,
            User = input.User,
            Password = input.Password,
            Repository = input.Repository,
            Adapter = input.Adapter
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
        [Service] IConnectionRepository repository)
    {
        var connection = new Connection
        {
            Id = id,
            ClientName = input.ClientName,
            Servidor = input.Servidor,
            Puerto = input.Puerto,
            User = input.User,
            Password = input.Password,
            Repository = input.Repository,
            Adapter = input.Adapter
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
}

public record ConnectionInput(
    string ClientName,
    string Servidor,
    string Puerto,
    string User,
    string Password,
    string Repository,
    string Adapter
);

public class LoginResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Token { get; set; }
}
