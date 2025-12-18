using System.Text;
using DataNath.ApiMetadatos.Configuration;
using DataNath.ApiMetadatos.GraphQL;
using DataNath.ApiMetadatos.Repositories;
using DataNath.ApiMetadatos.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Configurar CosmosDbSettings desde appsettings.json
builder.Services.Configure<CosmosDbSettings>(
    builder.Configuration.GetSection("CosmosDb"));

// Configurar EncryptionSettings desde appsettings.json
builder.Services.Configure<EncryptionSettings>(
    builder.Configuration.GetSection("Encryption"));

// Configurar JwtSettings desde appsettings.json
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

// Registrar CosmosClient como Singleton
builder.Services.AddSingleton<CosmosClient>(serviceProvider =>
{
    var settings = builder.Configuration.GetSection("CosmosDb").Get<CosmosDbSettings>();

    if (settings == null)
        throw new InvalidOperationException("CosmosDb configuration is missing");

    return new CosmosClient(settings.Endpoint, settings.Key);
});

// Registrar servicios
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSingleton<DatabaseMetadataServiceFactory>();

// Registrar repositorios
builder.Services.AddScoped<IConnectionRepository, ConnectionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPersistentRequirementRepository, PersistentRequirementRepository>();

// Configurar autenticación JWT
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();

if (jwtSettings == null)
    throw new InvalidOperationException("JWT configuration is missing");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings.SigningKey))
        };
    });

builder.Services.AddAuthorization();

// Configurar CORS - Configuración específica para Angular
builder.Services.AddCors(options =>
{
    options.AddPolicy("PublicApiPolicy", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200", "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configurar GraphQL con soporte CORS
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddAuthorization()
    .ModifyRequestOptions(opt =>
    {
        opt.IncludeExceptionDetails = true;
    });

var app = builder.Build();

// Configurar el pipeline HTTP
app.UseRouting();
app.UseCors("PublicApiPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Mapear el endpoint de GraphQL con CORS explícito
app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL("/graphql").RequireCors("PublicApiPolicy");
});

app.Run();
