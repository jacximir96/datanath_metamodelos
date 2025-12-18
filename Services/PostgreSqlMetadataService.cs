using DataNath.ApiMetadatos.Models;
using Npgsql;

namespace DataNath.ApiMetadatos.Services;

public class PostgreSqlMetadataService : IDatabaseMetadataService
{
    public async Task<List<string>> GetTablesAsync(DatabaseConnectionInfo connection)
    {
        var tables = new List<string>();
        var connectionString = BuildConnectionString(connection);

        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var query = @"
            SELECT tablename
            FROM pg_tables
            WHERE schemaname = 'public'
            ORDER BY tablename";

        using var command = new NpgsqlCommand(query, conn);
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

        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var query = @"
            SELECT
                c.column_name,
                c.data_type,
                c.character_maximum_length,
                c.is_nullable,
                CASE WHEN pk.column_name IS NOT NULL THEN true ELSE false END AS is_primary_key,
                c.column_default
            FROM information_schema.columns c
            LEFT JOIN (
                SELECT ku.column_name
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage ku
                    ON tc.constraint_name = ku.constraint_name
                    AND tc.table_schema = ku.table_schema
                WHERE tc.constraint_type = 'PRIMARY KEY'
                    AND tc.table_name = @tableName
                    AND tc.table_schema = 'public'
            ) pk ON c.column_name = pk.column_name
            WHERE c.table_name = @tableName
                AND c.table_schema = 'public'
            ORDER BY c.ordinal_position";

        using var command = new NpgsqlCommand(query, conn);
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
                IsPrimaryKey = reader.GetBoolean(4),
                DefaultValue = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return columns;
    }

    public async Task<List<RelationInfo>> GetTableRelationsAsync(DatabaseConnectionInfo connection, string tableName)
    {
        var relations = new List<RelationInfo>();
        var connectionString = BuildConnectionString(connection);

        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var query = @"
            -- Foreign keys FROM this table
            SELECT
                tc.constraint_name AS relation_name,
                tc.table_name AS from_table,
                kcu.column_name AS from_column,
                ccu.table_name AS to_table,
                ccu.column_name AS to_column,
                'ForeignKey' AS relation_type
            FROM information_schema.table_constraints AS tc
            JOIN information_schema.key_column_usage AS kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage AS ccu
                ON ccu.constraint_name = tc.constraint_name
                AND ccu.table_schema = tc.table_schema
            WHERE tc.constraint_type = 'FOREIGN KEY'
                AND tc.table_name = @tableName
                AND tc.table_schema = 'public'

            UNION ALL

            -- Foreign keys TO this table (referenced by)
            SELECT
                tc.constraint_name AS relation_name,
                ccu.table_name AS from_table,
                ccu.column_name AS from_column,
                tc.table_name AS to_table,
                kcu.column_name AS to_column,
                'ReferencedBy' AS relation_type
            FROM information_schema.table_constraints AS tc
            JOIN information_schema.key_column_usage AS kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
            JOIN information_schema.constraint_column_usage AS ccu
                ON ccu.constraint_name = tc.constraint_name
                AND ccu.table_schema = tc.table_schema
            WHERE tc.constraint_type = 'FOREIGN KEY'
                AND ccu.table_name = @tableName
                AND tc.table_schema = 'public'";

        using var command = new NpgsqlCommand(query, conn);
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

    private string BuildConnectionString(DatabaseConnectionInfo connection)
    {
        return $"Host={connection.Servidor};" +
               $"Port={connection.Puerto};" +
               $"Database={connection.Repository};" +
               $"Username={connection.User};" +
               $"Password={connection.Password};";
    }
}
