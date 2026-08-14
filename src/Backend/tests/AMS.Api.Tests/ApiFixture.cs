using AMS.Modules.Assets.Persistence;
using AMS.Modules.Identity.Domain;
using AMS.Modules.Identity.Persistence;
using AMS.Modules.Organization.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AMS.Api.Tests;

/// <summary>
/// The real host, on a real database, reached over HTTP.
/// </summary>
/// <remarks>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> runs <c>Program.cs</c>
/// exactly as it runs in production — same DI, same middleware order, same
/// authentication. That is the point: every other test in this solution calls
/// a handler directly and so proves nothing about the wiring.
/// </remarks>
public sealed class ApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string Instance = @".\SQLEXPRESS2022";
    private const string Database = "AMS_ApiTests";

    public static string ConnectionString =>
        $"Server={Instance};Database={Database};Integrated Security=true;"
        + "TrustServerCertificate=true;MultipleActiveResultSets=true";

    public async ValueTask InitializeAsync()
    {
        await DropDatabaseAsync();
        await ExecuteOnMasterAsync($"CREATE DATABASE [{Database}];");

        // All three modules, because a request that spans them is one of the
        // things being tested.
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<OrganizationDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<AssetsDbContext>().Database.MigrateAsync();
    }

    // No DisposeAsync of our own. WebApplicationFactory already declares that
    // exact signature, so xunit's IAsyncLifetime.DisposeAsync cannot also be
    // implemented — and it does not need to be: InitializeAsync drops and
    // recreates the database, so a run always starts clean whatever the last
    // one left behind. The cost is one idle test database on the instance.

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Ams", ConnectionString);
        builder.UseSetting("Jwt:Issuer", "ams-tests");
        builder.UseSetting("Jwt:Audience", "ams-tests");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-long-enough");
    }

    /// <summary>Empties Identity so one test cannot see another's users.</summary>
    public static async Task ResetIdentityAsync() => await ExecuteAsync("""
        DELETE FROM [Identity].[UserRecoveryCode];
        DELETE FROM [Identity].[UserCapabilityOverride];
        DELETE FROM [Identity].[UserBranch];
        DELETE FROM [Identity].[UserRole];
        DELETE FROM [Identity].[RoleCapability];
        DELETE FROM [Identity].[User];
        DELETE FROM [Identity].[Role];
        DELETE FROM [Identity].[Capability];
        """);

    /// <summary>Empties the Assets lookups the register tests create.</summary>
    public static async Task ResetAssetsAsync()
    {
        await ExecuteAsync("DELETE FROM [Assets].[AssetEvent];");
        await ExecuteAsync("ALTER TABLE [Assets].[Asset] SET (SYSTEM_VERSIONING = OFF);");
        await ExecuteAsync("DELETE FROM [Assets].[Asset]; DELETE FROM [Assets].[AssetHistory];");
        await ExecuteAsync("""
            ALTER TABLE [Assets].[Asset]
                SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [Assets].[AssetHistory]));
            """);
        await ExecuteAsync("DELETE FROM [Assets].[AssetStatus]; DELETE FROM [Assets].[AssetType];");
    }

    /// <summary>
    /// A user who can sign in, holding exactly the capabilities given.
    /// </summary>
    /// <remarks>
    /// Seeded through the database rather than through the API: creating a user
    /// needs a capability, and the first administrator has to come from
    /// somewhere. That somewhere is the design script's seed in production.
    /// </remarks>
    public async Task<string> AddUserAsync(
        string username,
        string password,
        params string[] capabilities)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<
            Modules.Identity.PublicApi.IPasswordHasher>();

        var user = new User
        {
            Username = username,
            DisplayName = username,
            PasswordHash = hasher.Hash(password),
            IsActive = true,
            IsLocked = false,
            MustChangePassword = false,
            MfaEnabled = false,
            HasAllBranches = true,
            FailedLoginAttempts = 0,
            MfaEnrollmentRequired = false,
            CreatedOnUtc = DateTime.UtcNow,
            CreatedBy = "test",
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        if (capabilities.Length > 0)
        {
            var role = new Role
            {
                RoleName = $"role-for-{username}",
                IsActive = true,
                CreatedOnUtc = DateTime.UtcNow,
                CreatedBy = "test",
            };
            db.Roles.Add(role);
            await db.SaveChangesAsync();

            foreach (var capability in capabilities)
            {
                db.Capabilities.Add(new Capability
                {
                    Name = capability,
                    Module = "Assets",
                    Description = capability,
                    CreatedOnUtc = DateTime.UtcNow,
                    CreatedBy = "test",
                });
                db.RoleCapabilities.Add(new RoleCapability
                {
                    RoleId = role.Id,
                    CapabilityName = capability,
                });
            }

            db.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
            });
            await db.SaveChangesAsync();
        }

        return username;
    }

    private static async Task ExecuteAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync() => await ExecuteOnMasterAsync($"""
        IF DB_ID('{Database}') IS NOT NULL
        BEGIN
            ALTER DATABASE [{Database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [{Database}];
        END
        """);

    private static async Task ExecuteOnMasterAsync(string sql)
    {
        await using var connection = new SqlConnection(
            $"Server={Instance};Database=master;Integrated Security=true;TrustServerCertificate=true");
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(nameof(ApiCollectionDefinition))]
public sealed class ApiCollectionDefinition : ICollectionFixture<ApiFixture>;
