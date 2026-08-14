using AMS.Modules.DataImport.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AMS.Modules.DataImport;

/// <summary>
/// The module's single registration point. <c>Program.cs</c> is a list of
/// these calls and nothing else (docs/02 §9).
/// </summary>
public static class DataImportModuleExtensions
{
    public static IServiceCollection AddDataImportModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Ams")
            ?? throw new InvalidOperationException("Connection string 'Ams' is not configured.");

        services.AddDbContext<DataImportDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", DataImportDbContext.SchemaName)));

        return services;
    }
}
