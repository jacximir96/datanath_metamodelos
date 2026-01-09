using DataNath.ApiMetadatos.Models;

namespace DataNath.ApiMetadatos.Services;

public interface IDatabaseMetadataService
{
    /// <summary>
    /// Obtiene la lista de tablas/colecciones de la base de datos
    /// </summary>
    Task<List<string>> GetTablesAsync(DatabaseConnectionInfo connection);

    /// <summary>
    /// Obtiene las columnas/campos de una tabla/colección específica
    /// </summary>
    Task<List<ColumnInfo>> GetTableColumnsAsync(DatabaseConnectionInfo connection, string tableName);

    /// <summary>
    /// Obtiene las relaciones (foreign keys) de una tabla específica
    /// </summary>
    Task<List<RelationInfo>> GetTableRelationsAsync(DatabaseConnectionInfo connection, string tableName);

    /// <summary>
    /// Obtiene los valores distintos de un campo específico en una tabla/colección
    /// </summary>
    Task<List<string>> GetDistinctValuesAsync(DatabaseConnectionInfo connection, string tableName, string fieldName);
}
