using DataNath.ApiMetadatos.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DataNath.ApiMetadatos.Services;

public class MongoDbMetadataService : IDatabaseMetadataService
{
    public async Task<List<string>> GetTablesAsync(DatabaseConnectionInfo connection)
    {
        var client = CreateMongoClient(connection);
        var database = client.GetDatabase(connection.Repository);

        var collections = await database.ListCollectionNamesAsync();
        var tables = new List<string>();

        await collections.ForEachAsync(collection => tables.Add(collection));

        return tables.OrderBy(t => t).ToList();
    }

    public async Task<List<ColumnInfo>> GetTableColumnsAsync(DatabaseConnectionInfo connection, string tableName)
    {
        var client = CreateMongoClient(connection);
        var database = client.GetDatabase(connection.Repository);
        var collection = database.GetCollection<BsonDocument>(tableName);

        // En MongoDB, analizamos documentos de muestra para inferir el schema
        var sampleDocuments = await collection.Find(new BsonDocument())
            .Limit(100)
            .ToListAsync();

        if (!sampleDocuments.Any())
        {
            return new List<ColumnInfo>();
        }

        // Extraer todos los campos únicos de los documentos de muestra (incluyendo anidados)
        var fieldsMap = new Dictionary<string, HashSet<string>>();

        foreach (var doc in sampleDocuments)
        {
            ExtractFieldsRecursive(doc, "", fieldsMap);
        }

        var columns = fieldsMap.Select(kvp => new ColumnInfo
        {
            ColumnName = kvp.Key,
            DataType = string.Join(" | ", kvp.Value),
            MaxLength = null,
            IsNullable = true, // MongoDB permite null por defecto
            IsPrimaryKey = kvp.Key == "_id",
            DefaultValue = null
        })
        .OrderBy(c => c.ColumnName == "_id" ? 0 : 1)
        .ThenBy(c => c.ColumnName)
        .ToList();

        return columns;
    }

    private void ExtractFieldsRecursive(BsonDocument document, string prefix, Dictionary<string, HashSet<string>> fieldsMap)
    {
        foreach (var element in document.Elements)
        {
            var fieldName = string.IsNullOrEmpty(prefix) ? element.Name : $"{prefix}.{element.Name}";

            if (!fieldsMap.ContainsKey(fieldName))
            {
                fieldsMap[fieldName] = new HashSet<string>();
            }

            // Si es un documento anidado, extraer recursivamente
            if (element.Value.BsonType == BsonType.Document)
            {
                fieldsMap[fieldName].Add("Object");
                ExtractFieldsRecursive(element.Value.AsBsonDocument, fieldName, fieldsMap);
            }
            // Si es un array, analizar el primer elemento si existe
            else if (element.Value.BsonType == BsonType.Array)
            {
                fieldsMap[fieldName].Add("Array");
                var array = element.Value.AsBsonArray;
                if (array.Count > 0 && array[0].BsonType == BsonType.Document)
                {
                    ExtractFieldsRecursive(array[0].AsBsonDocument, fieldName, fieldsMap);
                }
            }
            else
            {
                fieldsMap[fieldName].Add(element.Value.BsonType.ToString());
            }
        }
    }

    public async Task<List<RelationInfo>> GetTableRelationsAsync(DatabaseConnectionInfo connection, string tableName)
    {
        // MongoDB no tiene relaciones explícitas (foreign keys)
        // Podríamos inferir relaciones basadas en campos que terminan en "Id" o "_id"
        // Por ahora retornamos lista vacía
        await Task.CompletedTask;
        return new List<RelationInfo>();
    }

    public async Task<List<string>> GetDistinctValuesAsync(DatabaseConnectionInfo connection, string tableName, string fieldName)
    {
        var client = CreateMongoClient(connection);
        var database = client.GetDatabase(connection.Repository);
        var collection = database.GetCollection<BsonDocument>(tableName);

        var distinctValues = await collection.Distinct<string>(fieldName, FilterDefinition<BsonDocument>.Empty).ToListAsync();
        return distinctValues.OrderBy(v => v).ToList();
    }

    private MongoClient CreateMongoClient(DatabaseConnectionInfo connection)
    {
        // Detectar si es MongoDB Atlas (usa mongodb+srv:// sin puerto)
        var isAtlas = connection.Servidor.Contains("mongodb.net", StringComparison.OrdinalIgnoreCase);

        string connectionString;

        if (isAtlas)
        {
            // MongoDB Atlas usa mongodb+srv:// sin puerto
            connectionString = string.IsNullOrEmpty(connection.User)
                ? $"mongodb+srv://{connection.Servidor}"
                : $"mongodb+srv://{connection.User}:{connection.Password}@{connection.Servidor}/?retryWrites=true&w=majority";
        }
        else
        {
            // MongoDB local/on-premise usa mongodb:// con puerto
            var puerto = string.IsNullOrEmpty(connection.Puerto) ? "27017" : connection.Puerto;
            connectionString = string.IsNullOrEmpty(connection.User)
                ? $"mongodb://{connection.Servidor}:{puerto}"
                : $"mongodb://{connection.User}:{connection.Password}@{connection.Servidor}:{puerto}";
        }

        return new MongoClient(connectionString);
    }
}
