"""
Generates EF Core entities, configurations and DbContexts from design-model.json.

Why generated: 87 tables mirrored by hand is 87 chances to mistype a max length
or drop a filter, and the schema-parity check would then surface them one at a
time over days. The generator makes the same decision 87 times, and
build/Compare-Schema.ps1 proves each module against the reviewed script.

Why NOT generated forever: these are persistence-faithful entities, not a
domain model. Behaviour is added by hand as slices are built - a domain method
belongs to the module that owns it, and nothing here should be regenerated over
the top of it. Regeneration is for the initial import only; after that the
files are ordinary source.

Identity is skipped: it was written by hand as the reference implementation and
is already proven at 85 objects.
"""
import json
import keyword
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
MODEL = Path(__file__).resolve().parent / "design-model.json"
MODULES = ROOT / "src" / "Backend" / "Modules"

SKIP_SCHEMAS = {"Identity"}

CSHARP_KEYWORDS = set(keyword.kwlist) | {
    "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
    "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
    "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
    "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
    "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
    "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
    "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
    "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
    "void", "volatile", "while",
}


def clr_type(sql_type: str, nullable: bool):
    """Maps a SQL type to (clr type, max length, precision, scale)."""
    t = sql_type.lower()
    length = precision = scale = None

    m = re.match(r"n?varchar\((max|\d+)\)", t)
    if m:
        length = None if m.group(1) == "max" else int(m.group(1))
        return ("string?" if nullable else "string"), length, None, None

    m = re.match(r"decimal\((\d+),(\d+)\)", t)
    if m:
        precision, scale = int(m.group(1)), int(m.group(2))
        return ("decimal?" if nullable else "decimal"), None, precision, scale

    if t == "rowversion":
        return "byte[]", None, None, None
    if t.startswith("varbinary"):
        return ("byte[]?" if nullable else "byte[]"), None, None, None

    simple = {
        "int": "int", "bigint": "long", "tinyint": "byte", "smallint": "short",
        "bit": "bool", "uniqueidentifier": "Guid", "date": "DateOnly",
        "datetime2": "DateTime", "float": "double", "real": "float", "money": "decimal",
    }
    if t.startswith("time("):
        base = "TimeOnly"
    elif t.startswith("datetime2("):
        base = "DateTime"
    else:
        base = simple.get(t)

    if base is None:
        raise ValueError(f"unmapped SQL type: {sql_type}")

    return (base + "?" if nullable else base), None, None, None


def default_call(default: dict) -> str:
    """The HasDefaultValueSql call for a column DEFAULT, carrying its NAME.

    The name matters. Without the second argument SQL Server invents one -
    DF__AssetType__IsAll__395884C4 - which differs on every database it is
    created on, so the design script and the EF model can never be compared on
    equal terms and Compare-Schema.ps1 reports 46 mismatches that are really
    one missing argument.
    """
    expr = default["expression"].replace("\\", "\\\\").replace('"', '\\"')
    return f'HasDefaultValueSql("{expr}", "{default["name"]}")'


def clr_literal(default: dict, clr: str):
    """The C# literal matching a SQL DEFAULT, or None if there is no sensible one.

    Only for NON-NULLABLE columns. The point is to make the entity's own default
    agree with the database's, because EF treats a column with a default as
    store-generated and omits it from the INSERT whenever the property still
    holds the CLR default.

    That is not a subtlety, it is a silent data loss: [IsAllocatable] defaults
    to 1, `false` is the CLR default for bool, so `new AssetType { IsAllocatable
    = false }` sent no column at all and the row came back allocatable. Seven
    booleans and two integers in this schema behaved that way. Pairing this with
    ValueGeneratedNever() below makes EF always send what the caller asked for,
    and leaves the database default where it belongs - for the design script and
    for importers writing raw SQL.
    """
    expr = default["expression"].strip().strip("()").strip()
    if clr == "bool":
        if expr in {"0", "1"}:
            return "true" if expr == "1" else "false"
        return None
    if clr in {"int", "long", "short", "byte"}:
        return expr if expr.lstrip("-").isdigit() else None
    if clr == "decimal":
        return f"{expr}m" if expr.lstrip("-").replace(".", "", 1).isdigit() else None
    if clr == "string" and expr.startswith(("N'", "'")) and expr.endswith("'"):
        return '"' + expr[expr.index("'") + 1:-1].replace('"', '\\"') + '"'
    # newid(), getutcdate() and anything else stay store-generated: there is no
    # literal for them and the database is the right place to produce one.
    return None


# The literals C# already gives a field for free. Writing them out is what
# CA1805 objects to, and it is right: `= false` on a bool says nothing.
CLR_DEFAULTS = {"false", "0", "0m", '""'}


def needs_initializer(literal) -> bool:
    return literal is not None and literal not in CLR_DEFAULTS


def safe(name: str) -> str:
    return "@" + name if name in CSHARP_KEYWORDS else name


def plural(name: str) -> str:
    if name.endswith("y") and not name.endswith(("ay", "ey", "iy", "oy", "uy")):
        return name[:-1] + "ies"
    if name.endswith(("s", "x", "z", "ch", "sh")):
        return name + "es"
    return name + "s"


def xml_escape(text: str) -> str:
    return text.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def entity_source(table, ns):
    lines = [f"namespace {ns}.Domain;", "", "/// <summary>",
             f"/// Mirrors <c>[{table['schema']}].[{table['name']}]</c> in AMS_Consolidated_Design_v2.sql.",
             "/// </summary>"]
    if table["temporal"]:
        h = table["historyTable"]
        lines += ["/// <remarks>",
                  f"/// System-versioned. Prior versions live in <c>[{h['schema']}].[{h['name']}]</c>,",
                  "/// readable with <c>TemporalAsOf</c>. The concurrency token is",
                  "/// <c>ConcurrencyStamp</c>, NOT the period columns (R2-22).",
                  "/// </remarks>"]
    lines += [f"public sealed class {table['name']}", "{"]

    body = []
    for col in table["columns"]:
        clr, *_ = clr_type(col["sqlType"], col["nullable"])
        prop = safe(col["name"])
        literal = (clr_literal(col["default"], clr)
                   if col.get("default") and not col["nullable"] else None)
        if col["sqlType"].lower() == "rowversion":
            body.append("    /// <summary>Concurrency token. Never nullable (03 §1 rule 7a).</summary>")
            body.append(f"    public byte[] {prop} {{ get; set; }} = [];")
        elif needs_initializer(literal):
            # The database default, restated in C#, so the two cannot disagree.
            body.append(f"    /// <summary>Defaults to <c>{col['default']['expression']}</c>,"
                        f" as <c>{col['default']['name']}</c> does.</summary>")
            if clr == "string":
                body.append(f"    public string {prop} {{ get; set; }} = {literal};")
            else:
                body.append(f"    public {clr} {prop} {{ get; set; }} = {literal};")
        elif clr == "string":
            body.append(f"    public required string {prop} {{ get; set; }}")
        elif clr == "byte[]":
            body.append(f"    public byte[] {prop} {{ get; set; }} = [];")
        else:
            body.append(f"    public {clr} {prop} {{ get; set; }}")
        body.append("")
    if body and body[-1] == "":
        body.pop()

    lines += body + ["}", ""]
    return "\n".join(lines)


def configuration_source(table, ns):
    name = table["name"]
    lines = [f"using {ns}.Domain;",
             "using Microsoft.EntityFrameworkCore;",
             "using Microsoft.EntityFrameworkCore.Metadata.Builders;",
             "",
             f"namespace {ns}.Persistence.Configurations;",
             "",
             "/// <summary>",
             f"/// Mirrors <c>[{table['schema']}].[{name}]</c> in AMS_Consolidated_Design_v2.sql,",
             "/// including every constraint and index NAME (docs/03 §3).",
             "/// </summary>",
             f"public sealed class {name}Configuration : IEntityTypeConfiguration<{name}>",
             "{",
             f"    public void Configure(EntityTypeBuilder<{name}> builder)",
             "    {"]

    checks = table["checks"]
    if table["temporal"] or checks:
        lines.append(f'        builder.ToTable("{name}", table =>')
        lines.append("        {")
        for chk in checks:
            expr = chk["expression"].replace("\\", "\\\\").replace('"', '\\"')
            expr = re.sub(r"\s+", " ", expr).strip()
            lines.append(f'            table.HasCheckConstraint("{chk["name"]}", "{expr}");')
        if table["temporal"]:
            h = table["historyTable"]
            lines.append("            table.IsTemporal(temporal =>")
            lines.append("            {")
            lines.append('                temporal.HasPeriodStart("SysStartTime");')
            lines.append('                temporal.HasPeriodEnd("SysEndTime");')
            lines.append(f'                temporal.UseHistoryTable("{h["name"]}", "{h["schema"]}");')
            lines.append("            });")
        lines.append("        });")
    else:
        lines.append(f'        builder.ToTable("{name}");')
    lines.append("")

    pk = table["primaryKey"]
    by_name = {c["name"]: c for c in table["columns"]}
    if len(pk) == 1:
        lines.append(f'        builder.HasKey(x => x.{safe(pk[0])}).HasName("{table["primaryKeyName"]}");')
        key_col = by_name.get(pk[0])
        if key_col is not None and not key_col["identity"] and key_col["sqlType"].lower() in {
            "int", "bigint", "smallint", "tinyint",
        }:
            # EF makes an integer key IDENTITY by convention. Several tables here
            # take their key from another module (Discovery.AssetHealth is one row
            # per asset, keyed by the Assets module's id), so the value arrives
            # with the row and the database must not invent one.
            lines.append(f"        builder.Property(x => x.{safe(pk[0])}).ValueGeneratedNever();")
    else:
        cols = ", ".join(f"x.{safe(c)}" for c in pk)
        lines.append(f'        builder.HasKey(x => new {{ {cols} }}).HasName("{table["primaryKeyName"]}");')
    lines.append("")

    for col in table["columns"]:
        clr, length, precision, scale = clr_type(col["sqlType"], col["nullable"])
        prop = safe(col["name"])
        parts = []
        if col["sqlType"].lower() == "rowversion":
            lines.append(f"        builder.Property(x => x.{prop}).IsRowVersion();")
            continue
        if col["name"] == "ConcurrencyStamp":
            lines.append("        // R2-22: the token for a system-versioned table. SysStartTime is history only.")
            stamp = ["IsConcurrencyToken()"]
            # This used to `continue` here and drop the column's DEFAULT on the
            # floor, so all five ConcurrencyStamp columns reached the database
            # without their newid() default while the design script declared it.
            if col["default"]:
                stamp.append(default_call(col["default"]))
            lines.append(f"        builder.Property(x => x.{prop})." + ".".join(stamp) + ";")
            continue
        if length is not None:
            parts.append(f"HasMaxLength({length})")
        if precision is not None:
            parts.append(f"HasPrecision({precision}, {scale})")
        if clr == "byte[]":
            parts.append('HasColumnType("varbinary(max)")')
        if clr.startswith("string") and length is None and col["sqlType"].lower().endswith("(max)"):
            parts.append('HasColumnType("nvarchar(max)")')
        if not col["nullable"]:
            parts.append("IsRequired()")
        if col["default"]:
            parts.append(default_call(col["default"]))
            if clr_literal(col["default"], clr) is not None and not col["nullable"]:
                # EF omits a store-generated column whenever the property holds
                # the CLR default, so `IsAllocatable = false` on a column
                # defaulting to 1 sent nothing and the row came back allocatable.
                # The entity carries the same default now, so EF can send the
                # value every time and the caller is believed.
                parts.append("ValueGeneratedNever()")
        if parts:
            lines.append(f"        builder.Property(x => x.{prop})." + ".".join(parts) + ";")
    lines.append("")

    for fk in table["foreignKeys"]:
        ref = fk["refTable"]
        col = safe(fk["columns"][0])
        lines.append(f"        builder.HasOne<{ref}>()")
        lines.append("            .WithMany()")
        lines.append(f"            .HasForeignKey(x => x.{col})")
        lines.append(f"            .OnDelete(DeleteBehavior.{fk['deleteRule']})")
        lines.append(f'            .HasConstraintName("{fk["name"]}");')
        lines.append("")

    for idx in table["indexes"]:
        if len(idx["columns"]) == 1:
            expr = f"x => x.{safe(idx['columns'][0])}"
        else:
            expr = "x => new { " + ", ".join(f"x.{safe(c)}" for c in idx["columns"]) + " }"
        lines.append(f"        builder.HasIndex({expr})")
        if idx["unique"]:
            lines.append("            .IsUnique()")
        if idx["filter"]:
            filt = re.sub(r"\s+", " ", idx["filter"]).strip().replace("\\", "\\\\").replace('"', '\\"')
            lines.append(f'            .HasFilter("{filt}")')
        lines.append(f'            .HasDatabaseName("{idx["name"]}");')
        lines.append("")

    while lines and lines[-1] == "":
        lines.pop()
    lines += ["    }", "}", ""]
    return "\n".join(lines)


def sequence_lines(sequences):
    """The HasSequence calls for one module's sequences.

    Generated, not hand-written. Three were added to DbContexts by hand and the
    next regeneration silently deleted all three - the DbContext is a generated
    file, so anything it must contain has to come from the design script.
    """
    lines = []
    for s in sequences:
        lines.append("")
        lines.append(f'        modelBuilder.HasSequence<long>("{s["name"]}", SchemaName)')
        lines.append(f'            .StartsAt({s["start"]})')
        lines.append(f'            .IncrementsBy({s["increment"]});')
    return lines


def context_source(schema, tables, ns, sequences):
    sets = []
    for t in sorted(tables, key=lambda x: x["name"]):
        sets.append(f"    public DbSet<{t['name']}> {plural(t['name'])} => Set<{t['name']}>();")
        sets.append("")
    if sets:
        sets.pop()

    return "\n".join([
        f"using {ns}.Domain;",
        "using Microsoft.EntityFrameworkCore;",
        "using Microsoft.EntityFrameworkCore.Metadata.Conventions;",
        "",
        f"namespace {ns}.Persistence;",
        "",
        "/// <summary>",
        f"/// The {schema} module's context. Owns schema <c>[{schema}]</c> and nothing",
        "/// else (docs/01 §2 rule 1).",
        "/// </summary>",
        f"public sealed class {schema}DbContext(DbContextOptions<{schema}DbContext> options) : DbContext(options)",
        "{",
        "    /// <summary>The schema this module owns, and its migrations-history schema.</summary>",
        f'    public const string SchemaName = "{schema}";',
        "",
        *sets,
        "",
        "    /// <summary>",
        "    /// Drops EF's automatic index-per-foreign-key convention.",
        "    /// </summary>",
        "    /// <remarks>",
        "    /// The reviewed design decides its own indexes: it adds one where a",
        "    /// query needs it (IX_UserRole_RoleId, IX_RoleCapability_CapabilityName)",
        "    /// and leaves it out where nothing reads that way. Letting EF add one",
        "    /// per foreign key produced 14 indexes the script never asked for -",
        "    /// each of them a write cost on a table somebody measured.",
        "    /// </remarks>",
        "    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)",
        "    {",
        "        ArgumentNullException.ThrowIfNull(configurationBuilder);",
        "",
        "        configurationBuilder.Conventions.Remove<ForeignKeyIndexConvention>();",
        "    }",
        "",
        "    // The parameter must be named modelBuilder: CA1725 requires an override",
        "    // to keep the base member's parameter names, and warnings are errors.",
        "    protected override void OnModelCreating(ModelBuilder modelBuilder)",
        "    {",
        "        modelBuilder.HasDefaultSchema(SchemaName);",
        *sequence_lines(sequences),
        "",
        "        // This assembly only. A configuration from another module would put",
        "        // another schema's table under this context.",
        f"        modelBuilder.ApplyConfigurationsFromAssembly(typeof({schema}DbContext).Assembly);",
        "    }",
        "}",
        "",
    ])


def factory_source(schema, ns):
    return "\n".join([
        "using Microsoft.EntityFrameworkCore;",
        "using Microsoft.EntityFrameworkCore.Design;",
        "",
        f"namespace {ns}.Persistence;",
        "",
        "/// <summary>",
        "/// Used by <c>dotnet ef</c> only. Never by the running application, which",
        "/// builds this context on the connection shared by every module (01 rule 4a).",
        "/// </summary>",
        f"public sealed class {schema}DbContextFactory : IDesignTimeDbContextFactory<{schema}DbContext>",
        "{",
        "    private const string DefaultConnection =",
        '        @"Server=.\\SQLEXPRESS2022;Database=AMS_Design;Integrated Security=true;TrustServerCertificate=true";',
        "",
        f"    public {schema}DbContext CreateDbContext(string[] args)",
        "    {",
        '        var connection = Environment.GetEnvironmentVariable("AMS_MIGRATIONS_CONNECTION") ?? DefaultConnection;',
        "",
        f"        var options = new DbContextOptionsBuilder<{schema}DbContext>()",
        "            .UseSqlServer(connection, sql => sql.MigrationsHistoryTable(",
        f'                "__EFMigrationsHistory", {schema}DbContext.SchemaName))',
        "            .Options;",
        "",
        f"        return new {schema}DbContext(options);",
        "    }",
        "}",
        "",
    ])


def module_extensions_source(schema, ns):
    return "\n".join([
        f"using {ns}.Persistence;",
        "using Microsoft.EntityFrameworkCore;",
        "using Microsoft.Extensions.Configuration;",
        "using Microsoft.Extensions.DependencyInjection;",
        "",
        f"namespace {ns};",
        "",
        "/// <summary>",
        "/// The module's single registration point. <c>Program.cs</c> is a list of",
        "/// these calls and nothing else (docs/02 §9).",
        "/// </summary>",
        f"public static class {schema}ModuleExtensions",
        "{",
        f"    public static IServiceCollection Add{schema}Module(",
        "        this IServiceCollection services,",
        "        IConfiguration configuration)",
        "    {",
        "        ArgumentNullException.ThrowIfNull(services);",
        "        ArgumentNullException.ThrowIfNull(configuration);",
        "",
        '        var connectionString = configuration.GetConnectionString("Ams")',
        "            ?? throw new InvalidOperationException(\"Connection string 'Ams' is not configured.\");",
        "",
        f"        services.AddDbContext<{schema}DbContext>(options =>",
        "            options.UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable(",
        f'                "__EFMigrationsHistory", {schema}DbContext.SchemaName)));',
        "",
        "        return services;",
        "    }",
        "}",
        "",
    ])


def write(path: Path, text: str):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")


def write_once(path: Path, text: str) -> bool:
    """Writes only if the file does not exist. Returns True if it was written.

    ModuleExtensions and DbContextFactory are SCAFFOLDS, not mirrors of the
    schema. Every slice adds a handler registration, an endpoint mapping and a
    SqlErrorTranslator line to ModuleExtensions, so regenerating it silently
    deletes the module's whole composition root - which is exactly what a
    re-run of this script did to OrganizationModuleExtensions.cs: 24 handler
    registrations, 24 endpoint mappings and 8 unique-index translations, gone,
    with every test still green because the tests call handlers directly.

    Entities, configurations and the DbContext stay regenerable: they mirror
    the design script and hold no hand-written decisions.
    """
    if path.exists():
        return False
    write(path, text)
    return True


def main():
    model = json.loads(MODEL.read_text(encoding="utf-8"))
    tables_in = model["tables"] if isinstance(model, dict) else model
    sequences_in = model.get("sequences", []) if isinstance(model, dict) else []

    sequences_by_schema = {}
    for sequence in sequences_in:
        sequences_by_schema.setdefault(sequence["schema"], []).append(sequence)

    by_schema = {}
    for table in tables_in:
        by_schema.setdefault(table["schema"], []).append(table)

    total_entities = total_configs = 0
    for schema, tables in sorted(by_schema.items()):
        if schema in SKIP_SCHEMAS:
            print(f"{schema:15} skipped (hand-written reference implementation)")
            continue

        project = MODULES / f"AMS.Modules.{schema}"
        if not project.exists():
            raise SystemExit(f"no project for schema {schema}")
        ns = f"AMS.Modules.{schema}"

        for table in tables:
            write(project / "Domain" / f"{table['name']}.cs", entity_source(table, ns))
            write(project / "Persistence" / "Configurations" / f"{table['name']}Configuration.cs",
                  configuration_source(table, ns))
            total_entities += 1
            total_configs += 1

        write(project / "Persistence" / f"{schema}DbContext.cs",
              context_source(schema, tables, ns, sequences_by_schema.get(schema, [])))

        kept = []
        if not write_once(project / "Persistence" / f"{schema}DbContextFactory.cs", factory_source(schema, ns)):
            kept.append("DbContextFactory")
        if not write_once(project / f"{schema}ModuleExtensions.cs", module_extensions_source(schema, ns)):
            kept.append("ModuleExtensions")

        note = f"   (kept: {', '.join(kept)})" if kept else ""
        print(f"{schema:15} {len(tables):3} tables{note}")

    print(f"\nentities: {total_entities}   configurations: {total_configs}")


if __name__ == "__main__":
    main()
