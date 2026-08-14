"""Replace the scaffolded NamedDefaultConstraints migrations.

EF scaffolds a rename of a DEFAULT constraint as AlterColumn, which SQL Server
rewrites the column for - and refuses when any index depends on that column.
Nine columns here sit under a filtered index, so five of the nine migrations
died on error 5074.

Naming a constraint does not need the column touched at all. sp_rename does it
in place, cannot fail on a dependent index, and is idempotent. The old name is
auto-generated and therefore unknown when the migration is written, so each
block looks it up by COLUMN instead of by name.
"""
import json, pathlib, re, collections

ROOT = pathlib.Path(r"C:\Users\siddeswaran.s\source\repos\AMS")
model = json.load(open(ROOT / "build" / "design-model.json", encoding="utf-8"))
tables = model["tables"] if isinstance(model, dict) and "tables" in model else model
tables = list(tables.values()) if isinstance(tables, dict) else tables

by_schema = collections.defaultdict(list)
for t in tables:
    for c in t["columns"]:
        if c.get("default"):
            by_schema[t["schema"]].append(
                (t["schema"], t["name"], c["name"], c["default"]["name"], c["default"]["expression"]))


def rename_block(schema, table, column, target, expression):
    """SQL that gives the DEFAULT on one column the name the design gives it."""
    # sp_rename wants the OLD name schema-qualified - a bare constraint name is
    # ambiguous across schemas and fails with 15248 - and the NEW name bare,
    # because a rename cannot move an object between schemas.
    # Two cases, because not every default is merely misnamed. The five
    # ConcurrencyStamp columns never had one at all: generate_model.py used to
    # `continue` past the default when it saw a concurrency token, so no earlier
    # migration ever created it and there is nothing to rename.
    #
    # The qualified name is built into a variable first: EXEC takes variables,
    # not expressions, so passing N'[s].[' + @n + N']' inline is a syntax error.
    return (
        f"                DECLARE @n sysname, @q nvarchar(400);\n"
        f"                SELECT @n = dc.name FROM sys.default_constraints dc\n"
        f"                       JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id\n"
        f"                WHERE  dc.parent_object_id = OBJECT_ID(N'[{schema}].[{table}]') AND c.name = N'{column}';\n"
        f"                IF @n IS NULL\n"
        f"                    ALTER TABLE [{schema}].[{table}] ADD CONSTRAINT [{target}] DEFAULT {expression} FOR [{column}];\n"
        f"                ELSE IF @n <> N'{target}'\n"
        f"                BEGIN\n"
        f"                    SET @q = N'[{schema}].[' + @n + N']';\n"
        f"                    EXEC sp_rename @objname = @q, @newname = N'{target}', @objtype = N'OBJECT';\n"
        f"                END"
    )


BODY_UP = """            // HAND-REPLACED by build/rewrite_default_migrations.py.
            //
            // The scaffolder emitted AlterColumn for each of these, because to
            // EF a renamed DEFAULT constraint is a changed column. SQL Server
            // rewrites the column for an AlterColumn and refuses outright when
            // an index depends on it, so this migration failed with error 5074
            // on IX_AssetHandover_GrnQueue and eight other filtered indexes.
            //
            // Naming a constraint needs no column change. sp_rename does it in
            // place. The old name is one SQL Server invented - it differs on
            // every database - so each block finds the constraint by COLUMN,
            // and skips silently if it already carries the right name.
{blocks}
"""

BODY_DOWN = """            // Deliberately empty. Down() would have to restore names like
            // DF__AssetType__IsAll__395884C4, which SQL Server generated and
            // which differ per database, so there is nothing to restore TO.
            // Reverting this migration leaves the defaults correctly named,
            // which is harmless: the next Up() finds them already right.
"""

changed = 0
for schema, entries in sorted(by_schema.items()):
    module_dir = ROOT / "src" / "Backend" / "Modules" / f"AMS.Modules.{schema}" / "Persistence" / "Migrations"
    if not module_dir.exists():
        continue
    files = [f for f in module_dir.glob("*NamedDefaultConstraints.cs") if "Designer" not in f.name]
    if not files:
        continue
    path = files[0]
    text = path.read_text(encoding="utf-8")

    blocks = "\n".join(
        f'            migrationBuilder.Sql(@"\n{rename_block(s, t, c, n, e)}\n            ");'
        for s, t, c, n, e in sorted(entries)
    )

    new_up = BODY_UP.format(blocks=blocks)
    text = re.sub(
        r"(protected override void Up\(MigrationBuilder migrationBuilder\)\s*\n\s*\{\n).*?(\n        \}\n)",
        lambda m: m.group(1) + new_up + m.group(2),
        text, count=1, flags=re.S,
    )
    text = re.sub(
        r"(protected override void Down\(MigrationBuilder migrationBuilder\)\s*\n\s*\{\n).*?(\n        \}\n)",
        lambda m: m.group(1) + BODY_DOWN + m.group(2),
        text, count=1, flags=re.S,
    )
    path.write_text(text, encoding="utf-8")
    changed += 1
    print(f"{schema:<14} {len(entries):2d} defaults  ->  {path.name}")

print(f"\n{changed} migrations rewritten")
