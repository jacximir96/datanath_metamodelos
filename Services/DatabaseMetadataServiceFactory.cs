namespace DataNath.ApiMetadatos.Services;

public class DatabaseMetadataServiceFactory
{
    /// <summary>
    /// Crea el servicio de metadata apropiado según el tipo de adaptador
    /// </summary>
    public IDatabaseMetadataService CreateService(string adapter)
    {
        return adapter.ToLowerInvariant() switch
        {
            "sqlserver" => new SqlServerMetadataService(),
            "sqlserversp" => new SqlServerMetadataService(),
            "sql" => new SqlServerMetadataService(),

            "mongodb" => new MongoDbMetadataService(),
            "mongo" => new MongoDbMetadataService(),
            "mongolocal" => new MongoDbMetadataService(),
            "mongosrv" => new MongoDbMetadataService(),

            "cosmosdb" => new CosmosDbMetadataService(),
            "cosmos" => new CosmosDbMetadataService(),

            "postgresql" => new PostgreSqlMetadataService(),
            "postgres" => new PostgreSqlMetadataService(),
            "pgsql" => new PostgreSqlMetadataService(),

            _ => throw new NotSupportedException($"El adaptador '{adapter}' no está soportado. " +
                $"Adaptadores soportados: SqlServer, MongoDB, MongoSrv (Atlas), CosmosDB, PostgreSQL")
        };
    }
}
