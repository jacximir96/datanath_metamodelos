using DataNath.ApiMetadatos.Models;
using Microsoft.Data.SqlClient;

namespace DataNath.ApiMetadatos.Services;

public class SqlServerMetadataService : IDatabaseMetadataService
{
    public async Task<List<string>> GetTablesAsync(DatabaseConnectionInfo connection)
    {
        var tables = new List<string>();
        var connectionString = BuildConnectionString(connection);

        using var sqlConnection = new SqlConnection(connectionString);
        await sqlConnection.OpenAsync();

        var query = @"
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME";

        using var command = new SqlCommand(query, sqlConnection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    public async Task<List<ColumnInfo>> GetTableColumnsAsync(DatabaseConnectionInfo connection, string tableName)
    {
        var columns = new List<ColumnInfo>();
        var connectionString = BuildConnectionString(connection);

        using var sqlConnection = new SqlConnection(connectionString);
        await sqlConnection.OpenAsync();

        var query = @"
            SELECT
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.CHARACTER_MAXIMUM_LENGTH,
                c.IS_NULLABLE,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PRIMARY_KEY,
                c.COLUMN_DEFAULT
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN (
                SELECT ku.TABLE_NAME, ku.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS tc
                INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS ku
                    ON tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
                    AND tc.CONSTRAINT_NAME = ku.CONSTRAINT_NAME
            ) pk ON c.TABLE_NAME = pk.TABLE_NAME AND c.COLUMN_NAME = pk.COLUMN_NAME
            WHERE c.TABLE_NAME = @tableName
            ORDER BY c.ORDINAL_POSITION";

        using var command = new SqlCommand(query, sqlConnection);
        command.Parameters.AddWithValue("@tableName", tableName);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            columns.Add(new ColumnInfo
            {
                ColumnName = reader.GetString(0),
                DataType = reader.GetString(1),
                MaxLength = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                IsNullable = reader.GetString(3) == "YES",
                IsPrimaryKey = reader.GetInt32(4) == 1,
                DefaultValue = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return columns;
    }

    public async Task<List<RelationInfo>> GetTableRelationsAsync(DatabaseConnectionInfo connection, string tableName)
    {
        var relations = new List<RelationInfo>();
        var connectionString = BuildConnectionString(connection);

        using var sqlConnection = new SqlConnection(connectionString);
        await sqlConnection.OpenAsync();

        var query = @"
            SELECT
                fk.name AS RelationName,
                tp.name AS FromTable,
                cp.name AS FromColumn,
                tr.name AS ToTable,
                cr.name AS ToColumn,
                'ForeignKey' AS RelationType
            FROM sys.foreign_keys AS fk
            INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
            INNER JOIN sys.tables AS tp ON fkc.parent_object_id = tp.object_id
            INNER JOIN sys.columns AS cp ON fkc.parent_object_id = cp.object_id AND fkc.parent_column_id = cp.column_id
            INNER JOIN sys.tables AS tr ON fkc.referenced_object_id = tr.object_id
            INNER JOIN sys.columns AS cr ON fkc.referenced_object_id = cr.object_id AND fkc.referenced_column_id = cr.column_id
            WHERE tp.name = @tableName

            UNION ALL

            SELECT
                fk.name AS RelationName,
                tr.name AS FromTable,
                cr.name AS FromColumn,
                tp.name AS ToTable,
                cp.name AS ToColumn,
                'ReferencedBy' AS RelationType
            FROM sys.foreign_keys AS fk
            INNER JOIN sys.foreign_key_columns AS fkc ON fk.object_id = fkc.constraint_object_id
            INNER JOIN sys.tables AS tp ON fkc.referenced_object_id = tp.object_id
            INNER JOIN sys.columns AS cp ON fkc.referenced_object_id = cp.object_id AND fkc.referenced_column_id = cp.column_id
            INNER JOIN sys.tables AS tr ON fkc.parent_object_id = tr.object_id
            INNER JOIN sys.columns AS cr ON fkc.parent_object_id = cr.object_id AND fkc.parent_column_id = cr.column_id
            WHERE tp.name = @tableName";

        using var command = new SqlCommand(query, sqlConnection);
        command.Parameters.AddWithValue("@tableName", tableName);

        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            relations.Add(new RelationInfo
            {
                RelationName = reader.GetString(0),
                FromTable = reader.GetString(1),
                FromColumn = reader.GetString(2),
                ToTable = reader.GetString(3),
                ToColumn = reader.GetString(4),
                RelationType = reader.GetString(5)
            });
        }

        return relations;
    }

    public async Task<List<string>> GetDistinctValuesAsync(DatabaseConnectionInfo connection, string tableName, string fieldName)
    {
        var values = new List<string>();
        var connectionString = BuildConnectionString(connection);

        using var sqlConnection = new SqlConnection(connectionString);
        await sqlConnection.OpenAsync();

        var query = $@"
            SELECT DISTINCT [{fieldName}]
            FROM [{tableName}]
            WHERE [{fieldName}] IS NOT NULL
            ORDER BY [{fieldName}]";

        using var command = new SqlCommand(query, sqlConnection);
        using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            values.Add(reader.GetValue(0)?.ToString() ?? "");
        }

        return values;
    }

    private string BuildConnectionString(DatabaseConnectionInfo connection)
    {
        return $"Server={connection.Servidor},{connection.Puerto};" +
               $"Database={connection.Repository};" +
               $"User Id={connection.User};" +
               $"Password={connection.Password};" +
               $"TrustServerCertificate=True;";
    }
}
