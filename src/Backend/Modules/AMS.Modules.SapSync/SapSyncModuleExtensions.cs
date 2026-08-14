using AMS.Modules.SapSync.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AMS.Modules.SapSync;

/// <summary>
/// The module's single registration point. <c>Program.cs</c> is a list of
/// these calls and nothing else (docs/02 §9).
/// </summary>
public static class SapSyncModuleExtensions
{
    public static IServiceCollection AddSapSyncModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Ams")
            ?? throw new InvalidOperationException("Connection string 'Ams' is not configured.");

        services.AddDbContext<SapSyncDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", SapSyncDbContext.SchemaName)));

        return services;
    }
}
