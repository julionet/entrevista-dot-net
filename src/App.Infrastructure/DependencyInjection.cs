using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using App.Application.Ports.Output;
using App.Infrastructure.Persistence;
using App.Infrastructure.Persistence.Repositories;
using App.Infrastructure.Security;

namespace App.Infrastructure;

/// <summary>
/// Composição da camada de persistência. Para trocar de banco de dados: instale o pacote do provider
/// desejado (ex.: Microsoft.EntityFrameworkCore.SqlServer, Npgsql.EntityFrameworkCore.PostgreSQL),
/// adicione um novo case no switch abaixo chamando o Use&lt;Provider&gt; correspondente, e ajuste
/// "Database:Provider" e "ConnectionStrings:Default" no appsettings.json.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' não configurada.");

        services.AddDbContext<AppDbContext>(options =>
        {
            switch (provider.ToLowerInvariant())
            {
                case "sqlite":
                    options.UseSqlite(connectionString);
                    break;
                default:
                    throw new NotSupportedException($"Provider de banco de dados '{provider}' não suportado.");
            }
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }

    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        return services;
    }
}
