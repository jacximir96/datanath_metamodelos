using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DataNath.ApiMetadatos.Configuration;
using DataNath.ApiMetadatos.Repositories;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DataNath.ApiMetadatos.Services;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IUserRepository _userRepository;
    private readonly IEncryptionService _encryptionService;

    public JwtService(
        IOptions<JwtSettings> jwtSettings,
        IUserRepository userRepository,
        IEncryptionService encryptionService)
    {
        _jwtSettings = jwtSettings.Value;
        _userRepository = userRepository;
        _encryptionService = encryptionService;
    }

    public string GenerateToken(string username)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, "Administrator")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<bool> ValidateCredentialsAsync(string username, string password)
    {
        var user = await _userRepository.GetUserAsync(username, password);

        if (user == null)
            return false;

        var decryptedPassword = _encryptionService.Decrypt(user.Password);

        return user.Name == username && password == decryptedPassword;
    }
}
