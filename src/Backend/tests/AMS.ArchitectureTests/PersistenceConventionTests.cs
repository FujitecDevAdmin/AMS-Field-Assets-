using System.Reflection;
using System.Text.RegularExpressions;

namespace AMS.ArchitectureTests;

/// <summary>
/// docs/03DATABASEEFCORESTANDARDS.md §1 — conventions that must hold in every
/// module's entities, checked across all sixteen rather than trusted to
/// sixteen separate reviews.
/// </summary>
public sealed class PersistenceConventionTests
{
    private const string RowVersion = "RowVersion";

    /// <summary>
    /// Rule 7a. A nullable CLR property produces a nullable column, and R2-14
    /// made every rowversion column NOT NULL because the value is always
    /// generated. Caught here at build time rather than by the schema-parity
    /// check after a migration has already been written.
    /// </summary>
    [Fact]
    public void RowVersion_is_never_nullable()
    {
        var context = new NullabilityInfoContext();
        var violations = new List<string>();

        foreach (var module in Solution.ModuleAssemblies)
        {
            // Compiler-generated types (anonymous types inside migrations)
            // carry a RowVersion of generic parameter type and are not ours.
            foreach (var type in module.GetTypes().Where(t => !IsCompilerGenerated(t)))
            {
                var property = type.GetProperty(RowVersion, BindingFlags.Public | BindingFlags.Instance);

                if (property is null)
                {
                    continue;
                }

                if (property.PropertyType != typeof(byte[]))
                {
                    violations.Add($"{type.FullName}.{RowVersion} is {property.PropertyType.Name}, expected byte[]");
                    continue;
                }

                if (context.Create(property).ReadState == NullabilityState.Nullable)
                {
                    violations.Add($"{type.FullName}.{RowVersion} is declared byte[]? and must be byte[]");
                }
            }
        }

        violations.ShouldBeEmpty(
            "RowVersion must be `public byte[] RowVersion { get; set; } = [];` — never byte[]. "
            + "R2-14 declared these columns NOT NULL (03 §1 rule 7a).");
    }

    private static bool IsCompilerGenerated(Type type) =>
        type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)
        || type.Name.StartsWith('<');

    /// <summary>The five system-versioned tables (R2-1).</summary>
    private static readonly string[] TemporalEntities =
        ["Employee", "Asset", "Contract", "SlaPolicy", "LocationOperationalHour"];

    /// <summary>
    /// R2-1: SQL Server forbids a rowversion column on a system-versioned
    /// table. An entity carrying both would simply not deploy.
    /// </summary>
    [Fact]
    public void The_five_temporal_entities_have_no_RowVersion_property()
    {
        var violations = new List<string>();

        foreach (var module in Solution.ModuleAssemblies)
        {
            foreach (var type in module.GetTypes().Where(t => TemporalEntities.Contains(t.Name, StringComparer.Ordinal)))
            {
                if (type.GetProperty(RowVersion, BindingFlags.Public | BindingFlags.Instance) is not null)
                {
                    violations.Add(type.FullName!);
                }
            }
        }

        violations.ShouldBeEmpty(
            "A system-versioned entity declared RowVersion. SQL Server forbids it (R2-1); "
            + "ConcurrencyStamp is the token on those five tables (R2-22).");
    }

    /// <summary>
    /// R2-22. The five temporal entities carry a <c>ConcurrencyStamp</c>
    /// instead, because SysStartTime turned out not to change on every update.
    /// </summary>
    [Fact]
    public void The_five_temporal_entities_carry_a_ConcurrencyStamp()
    {
        var violations = new List<string>();

        foreach (var module in Solution.ModuleAssemblies)
        {
            foreach (var type in module.GetTypes().Where(t => TemporalEntities.Contains(t.Name, StringComparer.Ordinal)))
            {
                var stamp = type.GetProperty("ConcurrencyStamp", BindingFlags.Public | BindingFlags.Instance);

                if (stamp is null)
                {
                    violations.Add($"{type.FullName} has no ConcurrencyStamp");
                }
                else if (stamp.PropertyType != typeof(Guid))
                {
                    violations.Add($"{type.FullName}.ConcurrencyStamp is {stamp.PropertyType.Name}, expected Guid");
                }
            }
        }

        violations.ShouldBeEmpty(
            "A system-versioned entity is missing its ConcurrencyStamp (R2-22). SysStartTime is "
            + "stamped from the transaction start time and does not move inside a clock tick, so it "
            + "cannot detect a concurrent write.");
    }

    /// <summary>
    /// R2-22 again, from the other side: nothing may nominate a period column
    /// as a concurrency token any more.
    /// </summary>
    [Fact]
    public void No_entity_maps_SysStartTime_as_a_concurrency_token()
    {
        // Statement-scoped, not file-scoped. A temporal configuration legitimately
        // mentions SysStartTime (HasPeriodStart) AND IsConcurrencyToken (on
        // ConcurrencyStamp) in the same file; only the two appearing in ONE
        // statement is the mistake being guarded against.
        var offending = new Regex(
            @"SysStartTime[^;]*IsConcurrencyToken|IsConcurrencyToken[^;]*SysStartTime",
            RegexOptions.Singleline);

        var offenders = Solution.SourceProjects
            .Where(p => p.Key.StartsWith("AMS.Modules.", StringComparison.Ordinal))
            .SelectMany(p => p.Value.Directory!.GetFiles("*.cs", SearchOption.AllDirectories))
            .Where(f => !f.FullName.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => offending.IsMatch(StripComments(File.ReadAllText(f.FullName))))
            .Select(f => f.Name)
            .ToArray();

        offenders.ShouldBeEmpty(
            "SysStartTime is history, not concurrency (R2-22). Use ConcurrencyStamp.");
    }

    /// <summary>
    /// Removes <c>//</c> and <c>///</c> comments so prose about a rule is not
    /// mistaken for a breach of it — the configurations explain R2-22 in a
    /// comment directly above the ConcurrencyStamp mapping, and the first
    /// version of this test failed on its own explanation.
    /// </summary>
    private static string StripComments(string source) =>
        new Regex(@"//[^\n]*").Replace(source, string.Empty);

    /// <summary>
    /// docs/03 §1 rule 4: instants are UTC and say so in the name. A DateTime
    /// property named otherwise is either a local time nobody can interpret
    /// later, or a name that lies.
    /// </summary>
    /// <remarks>
    /// The suffix is <c>OnUtc</c> in almost every case, but the rule is
    /// satisfied by any <c>*Utc</c> ending: the script has
    /// <c>EffectiveFromUtc</c>, <c>EffectiveToUtc</c> and
    /// <c>NextOperationalStartUtc</c>, where inserting "On" would make the
    /// name worse English for no gain. What matters is that the reader can
    /// tell it is UTC without opening the schema.
    /// </remarks>
    [Fact]
    public void DateTime_properties_are_named_Utc()
    {
        var violations = new List<string>();

        foreach (var module in Solution.ModuleAssemblies)
        {
            foreach (var type in module.GetTypes().Where(t => t.Namespace?.Contains(".Domain", StringComparison.Ordinal) == true))
            {
                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var isInstant = property.PropertyType == typeof(DateTime)
                                 || property.PropertyType == typeof(DateTime?);

                    if (isInstant && !property.Name.EndsWith("Utc", StringComparison.Ordinal))
                    {
                        violations.Add($"{type.Name}.{property.Name}");
                    }
                }
            }
        }

        violations.ShouldBeEmpty(
            "A DateTime property does not end in Utc. Instants are UTC and say so (03 §1 rule 4); "
            + "a genuine calendar date should be DateOnly, and a wall-clock time TimeOnly.");
    }
}
