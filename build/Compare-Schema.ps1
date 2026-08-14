<#
.SYNOPSIS
    Proves the EF model produces the reviewed design.

.DESCRIPTION
    docs/03DATABASEEFCORESTANDARDS.md §5: "CI compares a fresh migrate-from-zero
    database with a fresh run of AMS_Consolidated_Design_v2.sql (schema-compare
    must be empty except the history tables' EF bookkeeping)."

    Two throwaway databases are built from nothing:

      AMS_FromScript     sqlcmd runs the design script
      AMS_FromMigrations dotnet ef database update runs the migrations

    Columns, indexes, primary keys, foreign keys and check constraints are read
    out of both and compared. Anything present in one and not the other is
    printed and the script exits non-zero.

    The design script is the reviewed reference; the EF model is what ships.
    This is the only thing that keeps the two from drifting apart quietly, and
    drift is exactly what a modular monolith with sixteen schemas invites.

.PARAMETER Instance
    SQL Server instance. Defaults to the local Express instance.

.PARAMETER Schemas
    Which schemas to compare. Defaults to every module that has migrations.

.EXAMPLE
    ./build/Compare-Schema.ps1
    ./build/Compare-Schema.ps1 -Schemas Identity,Organization
#>
[CmdletBinding()]
param(
    [string]   $Instance = '.\SQLEXPRESS2022',
    # Every module that has migrations. Keep this in step with src/Backend/Modules —
    # the default used to be Identity alone, so a bare run compared one schema
    # of six and still printed MATCH. A parity check nobody has to remember to
    # parameterise is the only kind that catches anything.
    [string[]] $Schemas  = @(
        'Identity', 'Organization', 'Assets', 'Allocations',
        'Movements', 'Transfers', 'ServiceDesk', 'ServiceLevel', 'Notifications',
        'Contracts', 'Verification', 'Discovery'),
    [string]   $FromScriptDb = 'AMS_FromScript',
    [string]   $FromMigrationsDb = 'AMS_FromMigrations'
)

$ErrorActionPreference = 'Stop'
$repoRoot   = Split-Path -Parent $PSScriptRoot
$scriptPath = Join-Path $repoRoot 'AMS_Consolidated_Design_v2.sql'

if (-not (Test-Path $scriptPath)) {
    throw "Design script not found at $scriptPath"
}

function Invoke-Master([string] $sql) {
    sqlcmd -S $Instance -E -b -Q $sql | Out-Null
}

function Reset-Database([string] $name) {
    Invoke-Master "IF DB_ID('$name') IS NOT NULL BEGIN ALTER DATABASE [$name] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$name]; END; CREATE DATABASE [$name];"
}

# The interrogation. Deliberately one query per object kind rather than a
# single wide join: when something differs, the row that differs should say
# what kind of thing it is without being decoded.
# Every catalog name is COLLATE'd explicitly. sys.* columns carry the server
# collation, the literals carry the database's, and concatenating the two
# raises "Cannot resolve collation conflict" on any server whose collation
# differs from the database's - which is most of them.
$inventoryQuery = @'
SET NOCOUNT ON;
SELECT 'COLUMN|' + s.name COLLATE DATABASE_DEFAULT + '.' + t.name COLLATE DATABASE_DEFAULT + '|' + c.name COLLATE DATABASE_DEFAULT + '|' +
       ty.name COLLATE DATABASE_DEFAULT +
       CASE WHEN ty.name LIKE '%char%' OR ty.name LIKE '%binary%'
            THEN '(' + CASE WHEN c.max_length = -1 THEN 'max'
                            WHEN ty.name LIKE 'n%' THEN CAST(c.max_length / 2 AS varchar(10))
                            ELSE CAST(c.max_length AS varchar(10)) END + ')'
            WHEN ty.name IN ('decimal','numeric')
            THEN '(' + CAST(c.precision AS varchar(10)) + ',' + CAST(c.scale AS varchar(10)) + ')'
            ELSE '' END + '|' +
       CASE WHEN c.is_nullable = 1 THEN 'NULL' ELSE 'NOT NULL' END + '|' +
       CASE WHEN c.is_identity = 1 THEN 'IDENTITY' ELSE '' END AS Line
FROM   sys.columns c
       JOIN sys.tables t   ON t.object_id = c.object_id
       JOIN sys.schemas s  ON s.schema_id = t.schema_id
       JOIN sys.types ty   ON ty.user_type_id = c.user_type_id
WHERE  s.name IN (SCHEMA_LIST)
       AND t.temporal_type <> 1
       AND t.name <> '__EFMigrationsHistory'
UNION ALL
SELECT 'INDEX|' + s.name COLLATE DATABASE_DEFAULT + '.' + t.name COLLATE DATABASE_DEFAULT + '|' + i.name COLLATE DATABASE_DEFAULT + '|' +
       CASE WHEN i.is_unique = 1 THEN 'UNIQUE' ELSE 'NONUNIQUE' END + '|' +
       ISNULL(i.filter_definition COLLATE DATABASE_DEFAULT, '') + '|' +
       STUFF((SELECT ',' + col.name COLLATE DATABASE_DEFAULT
              FROM   sys.index_columns ic
                     JOIN sys.columns col ON col.object_id = ic.object_id AND col.column_id = ic.column_id
              WHERE  ic.object_id = i.object_id AND ic.index_id = i.index_id AND ic.is_included_column = 0
              ORDER  BY ic.key_ordinal
              FOR XML PATH('')), 1, 1, '')
FROM   sys.indexes i
       JOIN sys.tables t  ON t.object_id = i.object_id
       JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE  s.name IN (SCHEMA_LIST)
       AND i.name IS NOT NULL
       AND t.temporal_type <> 1
       AND t.name <> '__EFMigrationsHistory'
UNION ALL
SELECT 'FK|' + s.name COLLATE DATABASE_DEFAULT + '.' + t.name COLLATE DATABASE_DEFAULT + '|' + fk.name COLLATE DATABASE_DEFAULT + '|' +
       fk.delete_referential_action_desc COLLATE DATABASE_DEFAULT
FROM   sys.foreign_keys fk
       JOIN sys.tables t  ON t.object_id = fk.parent_object_id
       JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE  s.name IN (SCHEMA_LIST)
UNION ALL
SELECT 'CHECK|' + s.name COLLATE DATABASE_DEFAULT + '.' + t.name COLLATE DATABASE_DEFAULT + '|' + cc.name COLLATE DATABASE_DEFAULT + '|' +
       REPLACE(REPLACE(cc.definition COLLATE DATABASE_DEFAULT, ' ', ''), 'N''', '''')
FROM   sys.check_constraints cc
       JOIN sys.tables t  ON t.object_id = cc.parent_object_id
       JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE  s.name IN (SCHEMA_LIST)
UNION ALL
-- DEFAULT constraints. Added after a parse_design.py bug dropped twelve of
-- them from the EF model and this script still reported an exact match on
-- 1,464 objects, because it compared columns, indexes, FKs and CHECKs but
-- never defaults. A missing default is not cosmetic: a NOT NULL column with a
-- CHECK on it and no default is a column no plain INSERT can satisfy, and the
-- whole point of this script is that nobody has to notice that by hand.
SELECT 'DEFAULT|' + s.name COLLATE DATABASE_DEFAULT + '.' + t.name COLLATE DATABASE_DEFAULT + '|' +
       c.name COLLATE DATABASE_DEFAULT + '|' + dc.name COLLATE DATABASE_DEFAULT + '|' +
       REPLACE(REPLACE(dc.definition COLLATE DATABASE_DEFAULT, ' ', ''), 'N''', '''')
FROM   sys.default_constraints dc
       JOIN sys.tables t   ON t.object_id = dc.parent_object_id
       JOIN sys.columns c  ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
       JOIN sys.schemas s  ON s.schema_id = t.schema_id
WHERE  s.name IN (SCHEMA_LIST)
       AND t.temporal_type <> 1
       AND t.name <> '__EFMigrationsHistory'
UNION ALL
-- SEQUENCES. Added after the design script's three reached production with
-- none of them in the EF model, while this script reported an exact match on
-- 1,665 objects - it compared columns, indexes, FKs, CHECKs and defaults and
-- never sequences. The Movements batch handler found it by trying to use one,
-- which is the expensive way round.
--
-- current_value is deliberately NOT compared: it moves every time a number is
-- drawn, so comparing it would make this script fail on any database anybody
-- had used.
SELECT 'SEQUENCE|' + s.name COLLATE DATABASE_DEFAULT + '.' + q.name COLLATE DATABASE_DEFAULT + '|' +
       CAST(q.start_value AS varchar(40)) + '|' + CAST(q.increment AS varchar(40)) + '|' +
       CASE WHEN q.is_cycling = 1 THEN 'CYCLE' ELSE 'NOCYCLE' END
FROM   sys.sequences q
       JOIN sys.schemas s ON s.schema_id = q.schema_id
WHERE  s.name IN (SCHEMA_LIST)
ORDER  BY Line;
'@

$schemaList = ($Schemas | ForEach-Object { "'$_'" }) -join ','
$inventoryQuery = $inventoryQuery.Replace('SCHEMA_LIST', $schemaList)
$queryFile = Join-Path ([System.IO.Path]::GetTempPath()) 'ams-inventory.sql'
Set-Content -Path $queryFile -Value $inventoryQuery -Encoding utf8

function Get-Inventory([string] $database) {
    # One column is selected, so no separator is needed; passing -s '' makes
    # sqlcmd complain about a missing argument.
    $raw = sqlcmd -S $Instance -d $database -E -b -h -1 -W -i $queryFile
    if ($LASTEXITCODE -ne 0) { throw "Inventory query failed against $database" }
    return $raw |
        Where-Object { $_ -and $_ -notmatch '^\(\d+ rows affected\)$' -and $_.Trim() -ne '' } |
        ForEach-Object { $_.Trim() } |
        Sort-Object
}

Write-Host "Building $FromScriptDb from AMS_Consolidated_Design_v2.sql ..." -ForegroundColor Cyan
Reset-Database $FromScriptDb
sqlcmd -S $Instance -d $FromScriptDb -E -b -I -i $scriptPath | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'The design script failed to run.' }

Write-Host "Building $FromMigrationsDb from EF migrations ..." -ForegroundColor Cyan
Reset-Database $FromMigrationsDb
$env:AMS_MIGRATIONS_CONNECTION =
    "Server=$Instance;Database=$FromMigrationsDb;Integrated Security=true;TrustServerCertificate=true"

foreach ($schema in $Schemas) {
    $project = Join-Path $repoRoot "src/Backend/Modules/AMS.Modules.$schema"
    if (-not (Test-Path $project)) { throw "No module project for schema $schema at $project" }
    dotnet ef database update --project $project --context "${schema}DbContext" --no-build | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Migrations failed for $schema" }
}

$fromScript     = Get-Inventory $FromScriptDb
$fromMigrations = Get-Inventory $FromMigrationsDb

$onlyInScript     = Compare-Object $fromScript $fromMigrations | Where-Object SideIndicator -eq '<=' | ForEach-Object InputObject
$onlyInMigrations = Compare-Object $fromScript $fromMigrations | Where-Object SideIndicator -eq '=>' | ForEach-Object InputObject

Write-Host ''
Write-Host "Schemas compared : $($Schemas -join ', ')"
Write-Host "Objects (script) : $($fromScript.Count)"
Write-Host "Objects (EF)     : $($fromMigrations.Count)"
Write-Host ''

if (-not $onlyInScript -and -not $onlyInMigrations) {
    Write-Host 'MATCH - the EF model produces the reviewed design.' -ForegroundColor Green
    exit 0
}

if ($onlyInScript) {
    Write-Host 'In the DESIGN SCRIPT but missing from the EF model:' -ForegroundColor Yellow
    $onlyInScript | ForEach-Object { Write-Host "  - $_" }
    Write-Host ''
}

if ($onlyInMigrations) {
    Write-Host 'In the EF model but NOT in the design script:' -ForegroundColor Yellow
    $onlyInMigrations | ForEach-Object { Write-Host "  + $_" }
    Write-Host ''
}

Write-Host 'MISMATCH - fix whichever is wrong BY DECISION, never by drift (03 §intro).' -ForegroundColor Red
exit 1
