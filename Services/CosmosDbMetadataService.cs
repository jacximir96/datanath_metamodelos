using DataNath.ApiMetadatos.Models;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json.Linq;

namespace DataNath.ApiMetadatos.Services;

public class CosmosDbMetadataService : IDatabaseMetadataService
{
    public async Task<List<string>> GetTablesAsync(DatabaseConnectionInfo connection)
    {
        var client = CreateCosmosClient(connection);
        var database = client.GetDatabase(connection.Repository);

        var iterator = database.GetContainerQueryIterator<Microsoft.Azure.Cosmos.ContainerProperties>();
        var containers = new List<string>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            containers.AddRange(response.Select(c => c.Id));
        }

        return containers.OrderBy(c => c).ToList();
    }

    public async Task<List<ColumnInfo>> GetTableColumnsAsync(DatabaseConnectionInfo connection, string tableName)
    {
        var client = CreateCosmosClient(connection);
        var database = client.GetDatabase(connection.Repository);
        var container = database.GetContainer(tableName);

        // En Cosmos DB, analizamos documentos de muestra para inferir el schema
        var query = new QueryDefinition("SELECT TOP 100 * FROM c");
        var iterator = container.GetItemQueryIterator<JObject>(query);

        var fieldsMap = new Dictionary<string, HashSet<string>>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();

            foreach (var doc in response)
            {
                foreach (var property in doc.Properties())
                {
                    if (!fieldsMap.ContainsKey(property.Name))
                    {
                        fieldsMap[property.Name] = new HashSet<string>();
                    }

                    var type = property.Value.Type.ToString();
                    fieldsMap[property.Name].Add(type);
                }
            }
        }

        var columns = fieldsMap.Select(kvp => new ColumnInfo
        {
            ColumnName = kvp.Key,
            DataType = string.Join(" | ", kvp.Value),
            MaxLength = null,
            IsNullable = true,
            IsPrimaryKey = kvp.Key == "id",
            DefaultValue = null
        })
        .OrderBy(c => c.ColumnName == "id" ? 0 : 1)
        .ThenBy(c => c.ColumnName)
        .ToList();

        return columns;
    }

    public async Task<List<RelationInfo>> GetTableRelationsAsync(DatabaseConnectionInfo connection, string tableName)
    {
        // Cosmos DB no tiene relaciones explícitas (foreign keys)
        // Similar a MongoDB, es una base de datos NoSQL
        await Task.CompletedTask;
        return new List<RelationInfo>();
    }

    private CosmosClient CreateCosmosClient(DatabaseConnectionInfo connection)
    {
        // connection.Servidor contiene el endpoint
        // connection.Password contiene la key
        return new CosmosClient(connection.Servidor, connection.Password);
    }
}
