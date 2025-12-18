namespace DataNath.ApiMetadatos.Services;

public interface IJwtService
{
    string GenerateToken(string username);
    Task<bool> ValidateCredentialsAsync(string username, string password);
}
