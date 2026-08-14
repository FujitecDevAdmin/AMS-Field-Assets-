"""
Parses AMS_Consolidated_Design_v2.sql into a structured model.

This is the input to build/generate_model.py, which writes the EF entities and
configurations. It exists because 87 tables mirrored by hand is 87 chances to
mistype a max length, and because the schema-parity check would then find them
one at a time.

Correctness notes, both learned the hard way:

  * A table body ends at ");" OR at ") WITH (SYSTEM_VERSIONING = ON (...));".
    A regex that only knows the first form runs past the end of every temporal
    table and swallows the NEXT table whole. That silently produced 82 of 87
    tables and looked like a complete parse.

  * Block comments are stripped first, but line comments are not: several
    columns carry "-- Assets.Asset, id only" and that trailing text is what
    tells a reader the column is a cross-module link.
"""
import json
import re
import sys
from pathlib import Path

SCRIPT = Path(__file__).resolve().parent.parent / "AMS_Consolidated_Design_v2.sql"

# Columns SQL Server generates and EF maps through IsTemporal, not as properties.
PERIOD_COLUMNS = {"SysStartTime", "SysEndTime"}


def strip_block_comments(sql: str) -> str:
    return re.sub(r"/\*.*?\*/", "", sql, flags=re.S)


def parse_sequences(sql: str) -> list[dict]:
    """Every CREATE SEQUENCE, as {schema, name, start, increment, cycle}.

    Sequences were invisible here, so they were invisible to the EF model, and
    Compare-Schema.ps1 did not compare them either: the script had three and the
    model had none while the check reported an exact match on 1,665 objects.
    Both halves are fixed now - this is the half that generates them.
    """
    pattern = re.compile(
        r"CREATE SEQUENCE \[(\w+)\]\.\[(\w+)\]"
        r"(?:\s+AS\s+\w+)?"
        r"\s+START WITH\s+(-?\d+)"
        r"\s+INCREMENT BY\s+(-?\d+)"
        r"(\s+NO\s+CYCLE|\s+CYCLE)?",
        re.I)

    return [
        {
            "schema": m.group(1),
            "name": m.group(2),
            "start": int(m.group(3)),
            "increment": int(m.group(4)),
            "cycle": bool(m.group(5)) and "NO" not in m.group(5).upper(),
        }
        for m in pattern.finditer(sql)
    ]


def strip_line_comments(text: str) -> str:
    """
    Remove "-- ..." to end of line.

    This MUST run before splitting a table body on commas. Several column
    comments contain one - "-- NEW  Allocations.AssetHandover, id only" - and
    splitting first tears the column in two, which is how AssetEvent appeared
    to have no primary key.
    """
    return re.sub(r"--[^\n]*", "", text)


def split_top_level(body: str):
    """Split a CREATE TABLE body on commas that are not inside parentheses."""
    parts, depth, current = [], 0, []
    for ch in body:
        if ch == "(":
            depth += 1
        elif ch == ")":
            depth -= 1
        if ch == "," and depth == 0:
            parts.append("".join(current))
            current = []
        else:
            current.append(ch)
    if current:
        parts.append("".join(current))
    return [p.strip() for p in parts if p.strip()]


def parse_tables(sql: str):
    tables = []
    # Non-greedy body, then EITHER a plain close OR a system-versioning close.
    pattern = re.compile(
        r"CREATE TABLE \[(\w+)\]\.\[(\w+)\]\s*\((.*?)\n\s*\)\s*(WITH\s*\(\s*SYSTEM_VERSIONING[^;]*?\))?\s*;",
        re.S,
    )
    for match in pattern.finditer(sql):
        schema, name, body, versioning = match.groups()
        table = {
            "schema": schema,
            "name": name,
            "columns": [],
            "primaryKey": None,
            "primaryKeyName": None,
            "checks": [],
            "foreignKeys": [],
            "indexes": [],
            "temporal": bool(versioning),
            "historyTable": None,
        }
        if versioning:
            m = re.search(r"HISTORY_TABLE\s*=\s*\[(\w+)\]\.\[(\w+)\]", versioning)
            if m:
                table["historyTable"] = {"schema": m.group(1), "name": m.group(2)}

        for part in split_top_level(strip_line_comments(body)):
            line = part.strip()
            if not line:
                continue

            if line.upper().startswith("PERIOD FOR SYSTEM_TIME"):
                continue

            if line.upper().startswith("CONSTRAINT"):
                cname = re.match(r"CONSTRAINT \[(\w+)\]", line).group(1)
                if "PRIMARY KEY" in line.upper():
                    cols = re.search(r"PRIMARY KEY\s*\((.*?)\)", line, re.S).group(1)
                    table["primaryKey"] = re.findall(r"\[(\w+)\]", cols)
                    table["primaryKeyName"] = cname
                elif "FOREIGN KEY" in line.upper():
                    fk_cols = re.findall(r"\[(\w+)\]", re.search(r"FOREIGN KEY\s*\((.*?)\)", line, re.S).group(1))
                    ref = re.search(r"REFERENCES \[(\w+)\]\.\[(\w+)\]\s*\((.*?)\)", line, re.S)
                    delete_rule = "NoAction"
                    if re.search(r"ON DELETE CASCADE", line, re.I):
                        delete_rule = "Cascade"
                    elif re.search(r"ON DELETE SET NULL", line, re.I):
                        delete_rule = "SetNull"
                    table["foreignKeys"].append({
                        "name": cname,
                        "columns": fk_cols,
                        "refSchema": ref.group(1),
                        "refTable": ref.group(2),
                        "refColumns": re.findall(r"\[(\w+)\]", ref.group(3)),
                        "deleteRule": delete_rule,
                    })
                elif "CHECK" in line.upper():
                    expr = line[line.upper().index("CHECK") + 5:].strip()
                    table["checks"].append({"name": cname, "expression": expr})
                continue

            m = re.match(r"\[(\w+)\]\s+([A-Za-z0-9_]+(?:\s*\([^)]*\))?)\s*(.*)$", line, re.S)
            if not m:
                continue
            col_name, col_type, rest = m.group(1), re.sub(r"\s+", "", m.group(2)), m.group(3)
            if col_name in PERIOD_COLUMNS:
                continue

            default = None
            # \s+ and not a literal space: the DDL aligns its DEFAULT clauses
            # into a column, so every constraint name shorter than the longest
            # one in its table is followed by padding. A single-space pattern
            # here silently dropped those defaults and kept only the longest -
            # which then reached EF as "no default" and, for a NOT NULL column
            # with a CHECK on it, as an insert that could never succeed.
            dm = re.search(r"CONSTRAINT \[(\w+)\]\s+DEFAULT\s*\((.*?)\)\s*$", rest, re.S)
            if dm:
                default = {"name": dm.group(1), "expression": dm.group(2).strip()}

            table["columns"].append({
                "name": col_name,
                "sqlType": col_type,
                "nullable": "NOT NULL" not in rest.upper(),
                "identity": "IDENTITY" in rest.upper(),
                "default": default,
            })

        tables.append(table)
    return tables


def parse_indexes(sql: str, tables):
    by_key = {(t["schema"], t["name"]): t for t in tables}
    # \s+ around ON, not a literal space: the approval extension wraps the
    # statement across lines, and 18 of its indexes are invisible otherwise.
    pattern = re.compile(
        r"CREATE\s+(UNIQUE\s+)?(?:CLUSTERED\s+|NONCLUSTERED\s+)?INDEX\s+\[(\w+)\]\s+ON\s+"
        r"\[(\w+)\]\.\[(\w+)\]\s*\((.*?)\)\s*(WHERE\s+(.*?))?;",
        re.S,
    )
    for match in pattern.finditer(sql):
        unique, name, schema, table, cols, _, filt = match.groups()
        target = by_key.get((schema, table))
        if target is None:
            print(f"  !! index {name} targets unknown table {schema}.{table}", file=sys.stderr)
            continue
        target["indexes"].append({
            "name": name,
            "unique": bool(unique),
            "columns": re.findall(r"\[(\w+)\]", cols),
            "filter": filt.strip() if filt else None,
        })
    return tables


def main():
    sql = strip_block_comments(SCRIPT.read_text(encoding="utf-8", errors="replace"))
    declared = len(re.findall(r"CREATE TABLE \[\w+\]\.\[\w+\]", sql))

    tables = parse_indexes(sql, parse_tables(sql))

    print(f"declared tables : {declared}")
    print(f"parsed tables   : {len(tables)}")
    if declared != len(tables):
        names = {(t["schema"], t["name"]) for t in tables}
        missing = [f"{s}.{n}" for s, n in re.findall(r"CREATE TABLE \[(\w+)\]\.\[(\w+)\]", sql)
                   if (s, n) not in names]
        print("MISSING:", missing)
        sys.exit(1)

    print(f"temporal tables : {sum(1 for t in tables if t['temporal'])}")
    print(f"columns         : {sum(len(t['columns']) for t in tables)}")
    print(f"indexes         : {sum(len(t['indexes']) for t in tables)}")
    print(f"foreign keys    : {sum(len(t['foreignKeys']) for t in tables)}")
    print(f"check constraints: {sum(len(t['checks']) for t in tables)}")
    print(f"tables with no PK: {[t['name'] for t in tables if not t['primaryKey']]}")

    sequences = parse_sequences(sql)
    declared_sequences = len(re.findall(r"CREATE SEQUENCE", sql, re.I))
    print(f"sequences       : {len(sequences)} of {declared_sequences} declared")
    if len(sequences) != declared_sequences:
        print("MISSING: a CREATE SEQUENCE the pattern did not match")
        sys.exit(1)

    # A dict, not a bare list. It was a list of tables while tables were the
    # only thing here, and that shape is what made a sequence impossible to
    # express - so the file said the design had none.
    out = Path(__file__).resolve().parent / "design-model.json"
    out.write_text(
        json.dumps({"tables": tables, "sequences": sequences}, indent=2), encoding="utf-8")
    print(f"written         : {out}")


if __name__ == "__main__":
    main()
