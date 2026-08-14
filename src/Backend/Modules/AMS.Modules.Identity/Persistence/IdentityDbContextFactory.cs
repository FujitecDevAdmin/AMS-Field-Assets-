using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AMS.Modules.Identity.Persistence;

/// <summary>
/// Used by <c>dotnet ef</c> only. Never by the running application.
/// </summary>
/// <remarks>
/// <para>
/// At run time the context is built on the connection shared by every module
/// (01 rule 4a). The EF tools cannot get at that, so migrations get their own
/// connection here and the two never meet.
/// </para>
/// <para>
/// Override the target with <c>AMS_MIGRATIONS_CONNECTION</c> when generating
/// migrations against something other than the local Express instance.
/// </para>
/// </remarks>
public sealed class IdentityDbContextFactory : IDesignTimeDbContextFactory<IdentityDbContext>
{
    private const string DefaultConnection =
        @"Server=.\SQLEXPRESS2022;Database=AMS_Design;Integrated Security=true;TrustServerCertificate=true";

    public IdentityDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("AMS_MIGRATIONS_CONNECTION") ?? DefaultConnection;

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(connection, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", IdentityDbContext.SchemaName))
            .Options;

        return new IdentityDbContext(options);
    }
}
