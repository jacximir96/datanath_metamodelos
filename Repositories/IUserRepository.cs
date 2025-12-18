using DataNath.ApiMetadatos.Models;

namespace DataNath.ApiMetadatos.Repositories;

public interface IUserRepository
{
    Task<User?> GetUserAsync(string name, string password);
}
