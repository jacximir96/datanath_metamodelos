namespace DataNath.ApiMetadatos.Models;

public class DatabaseConnectionInfo
{
    public string Servidor { get; set; } = string.Empty;
    public string Puerto { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Adapter { get; set; } = string.Empty;
}
