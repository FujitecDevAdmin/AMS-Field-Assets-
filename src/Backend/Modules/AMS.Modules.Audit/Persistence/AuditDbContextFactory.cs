using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AMS.Modules.Audit.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> only. Never by the running application, which
/// builds this context on the connection shared by every module (01 rule 4a).
/// </summary>
public sealed class AuditDbContextFactory : IDesignTimeDbContextFactory<AuditDbContext>
{
    private const string DefaultConnection =
        @"Server=.\SQLEXPRESS2022;Database=AMS_Design;Integrated Security=true;TrustServerCertificate=true";

    public AuditDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("AMS_MIGRATIONS_CONNECTION") ?? DefaultConnection;

        var options = new DbContextOptionsBuilder<AuditDbContext>()
            .UseSqlServer(connection, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", AuditDbContext.SchemaName))
            .Options;

        return new AuditDbContext(options);
    }
}
