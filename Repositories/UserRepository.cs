using DataNath.ApiMetadatos.Configuration;
using DataNath.ApiMetadatos.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;

namespace DataNath.ApiMetadatos.Repositories;

public class UserRepository : IUserRepository
{
    private readonly Container _container;

    public UserRepository(CosmosClient cosmosClient, IOptions<CosmosDbSettings> settings)
    {
        var cosmosDbSettings = settings.Value;
        var database = cosmosClient.GetDatabase(cosmosDbSettings.DatabaseId);
        _container = database.GetContainer(cosmosDbSettings.UsersContainerId);
    }

    public async Task<Models.User?> GetUserAsync(string name, string password)
    {
        try
        {
            var query = new QueryDefinition(
                "SELECT * FROM c WHERE c.Name = @name")
                .WithParameter("@name", name);

            var iterator = _container.GetItemQueryIterator<Models.User>(query);

            if (iterator.HasMoreResults)
            {
                var response = await iterator.ReadNextAsync();
                return response.FirstOrDefault();
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
