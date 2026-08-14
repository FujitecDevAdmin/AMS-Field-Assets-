using AMS.Modules.Identity.Authentication;
using AMS.Modules.Identity.Domain;
using AMS.Modules.Identity.Persistence;
using AMS.Modules.Identity.PublicApi;
using AMS.Infrastructure.Security;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Tests;

/// <summary>A clock the tests own, so nothing depends on the wall clock.</summary>
public sealed class TestClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}

/// <summary>
/// The real clock, for the one collaborator that cannot be given a fake one.
/// </summary>
/// <remarks>
/// <see cref="MfaChallengeTokens"/> takes an <see cref="IClock"/> to stamp a
/// token's expiry, but ASP.NET's <c>ITimeLimitedDataProtector</c> checks that
/// expiry against the system clock and offers no way to override it. The two
/// must therefore agree, so this is the only clock a challenge token can be
/// issued with in a test.
/// </remarks>
public sealed class WallClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

/// <summary>A caller the tests own.</summary>
public sealed class TestCurrentUser : ICurrentUser
{
    public int Id { get; set; } = 1;

    public string Username { get; set; } = "test-admin";

    public int? EmployeeId { get; set; }

    public bool HasAllBranches { get; set; } = true;

    public IReadOnlySet<int> BranchIds { get; set; } = new HashSet<int>();

    public IReadOnlySet<string> Capabilities { get; set; } = new HashSet<string>();
}

/// <summary>
/// A real Identity schema, built by the module's own migrations.
/// </summary>
/// <remarks>
/// Migrations rather than a hand-written CREATE: the rules under test are the
/// ones the schema enforces, so the schema must be the one that ships. Each
/// test gets a clean set of tables.
/// </remarks>
public sealed class IdentityFixture : IAsyncLifetime
{
    private const string Instance = @".\SQLEXPRESS2022";
    private const string Database = "AMS_IdentityTests";

    public string ConnectionString { get; } =
        $"Server={Instance};Database={Database};Integrated Security=true;"
        + "TrustServerCertificate=true;MultipleActiveResultSets=true";

    public TestClock Clock { get; } = new();

    public TestCurrentUser CurrentUser { get; } = new();

    public IPasswordHasher Hasher { get; } = new Pbkdf2PasswordHasher();

    public ITotpCodes Totp => new TotpCodes(Clock);

    public ISecretProtector Secrets { get; private set; } = null!;

    public IMfaChallengeTokens Challenges { get; private set; } = null!;

    /// <summary>
    /// The real JWT issuer, with a throwaway key.
    /// </summary>
    /// <remarks>
    /// Not a stub. A sign-in that returns an unusable token is a sign-in that
    /// looks fine in tests and fails at the first request, and the claims it
    /// writes are what every other module's authorisation reads.
    /// </remarks>
    public IAccessTokens AccessTokens { get; } = new JwtAccessTokens(
        Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Issuer = "ams-tests",
            Audience = "ams-tests",
            SigningKey = "test-signing-key-that-is-long-enough-32",
        }),
        new WallClock());

    /// <summary>Resolves capabilities the way the sign-in path does.</summary>
    public static EffectiveAccess NewEffectiveAccess(IdentityDbContext context) => new(context);

    public SqlErrorTranslator SqlErrors { get; } = new SqlErrorTranslator()
        .Register("UX_User_Username", "User.UsernameTaken", "That username is already in use.")
        .Register("UX_User_Employee", "User.EmployeeAlreadyLinked", "That employee already has a login.")
        .Register("UX_UserBranch_OnePrimary", "User.OnePrimaryBranch", "A user can have only one primary branch.")
        .Register("UX_Role_Name", "Role.NameTaken", "A role with that name already exists.");

    public async ValueTask InitializeAsync()
    {
        await DropDatabaseAsync();
        await ExecuteOnMasterAsync($"CREATE DATABASE [{Database}];");

        await using var context = NewContext();
        await context.Database.MigrateAsync();

        var protection = DataProtectionProvider.Create(nameof(AMS.Modules.Identity.Tests));
        Secrets = new DataProtectionSecretProtector(protection);

        // NOT the fixture Clock. MfaChallengeTokens stamps the expiry from the
        // IClock it is given, but ITimeLimitedDataProtector.Unprotect judges
        // that expiry against the REAL system clock - there is no way to hand
        // it a different one. Giving it the frozen test clock therefore issued
        // tokens that expired at 09:05 on 12 Aug 2026 no matter when the suite
        // ran, so every MFA test passed for five minutes after that instant and
        // failed forever afterwards. In production the two clocks are the same
        // clock, which is why this only ever bit the tests.
        Challenges = new MfaChallengeTokens(protection, new WallClock());
    }

    public async ValueTask DisposeAsync() => await DropDatabaseAsync();

    public IdentityDbContext NewContext() =>
        new(new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", IdentityDbContext.SchemaName))
            .Options);

    /// <summary>Empties every table so one test cannot see another's rows.</summary>
    public async Task ResetAsync()
    {
        await ExecuteAsync("""
            DELETE FROM [Identity].[UserRecoveryCode];
            DELETE FROM [Identity].[UserCapabilityOverride];
            DELETE FROM [Identity].[UserBranch];
            DELETE FROM [Identity].[UserRole];
            DELETE FROM [Identity].[RoleCapability];
            DELETE FROM [Identity].[User];
            DELETE FROM [Identity].[Role];
            DELETE FROM [Identity].[Capability];
            """);
    }

    /// <summary>A signed-in-able user. Password defaults to something valid.</summary>
    public async Task<User> AddUserAsync(
        string username,
        string password = "correct horse battery",
        bool isActive = true,
        bool isLocked = false,
        bool mustChangePassword = false,
        bool mfaEnabled = false,
        string? mfaSecret = null)
    {
        await using var context = NewContext();

        var user = new User
        {
            Username = username,
            DisplayName = username,
            PasswordHash = Hasher.Hash(password),
            IsActive = isActive,
            IsLocked = isLocked,
            MustChangePassword = mustChangePassword,
            MfaEnabled = mfaEnabled,
            MfaSecretEncrypted = mfaSecret is null ? null : Secrets.Protect(mfaSecret),
            HasAllBranches = false,
            FailedLoginAttempts = 0,
            MfaEnrollmentRequired = false,
            CreatedOnUtc = Clock.UtcNow,
            CreatedBy = "test",
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task AddRecoveryCodeAsync(int userId, string code)
    {
        await using var context = NewContext();
        context.UserRecoveryCodes.Add(new UserRecoveryCode
        {
            UserId = userId,
            CodeHash = Hasher.Hash(code),
            CreatedOnUtc = Clock.UtcNow,
        });
        await context.SaveChangesAsync();
    }

    public async Task<User> ReloadAsync(int userId)
    {
        await using var context = NewContext();
        return await context.Users.SingleAsync(u => u.Id == userId);
    }

    public async Task ExecuteAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync() =>
        await ExecuteOnMasterAsync($"""
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

[CollectionDefinition(nameof(IdentityCollectionDefinition))]
public sealed class IdentityCollectionDefinition : ICollectionFixture<IdentityFixture>;
