# AMS — 03 Database & EF Core Standards (MSSQL + EF Core, Revision 3)

The target design is `AMS_Consolidated_Design_v2.sql`. The EF model is the source of truth for deployment (migrations), and **the migrations must produce exactly that script's design**. When code and script disagree, fix whichever is wrong *by decision*, never by drift — the script is the reviewed reference.

---

> **REVISION 3 HAS LANDED.** `[Assets]` is 18 tables and holds every asset the
> company owns. `AssetCategory` is **`AssetType`**; the accounting roll-up the
> FAR calls "Asset Category" is **`AssetClass`**, a separate axis, because 86
> technical groups appear under more than one class. `FieldAssets` is deleted —
> **fifteen modules, not sixteen**. Design:
> [`07ASSETREGISTERDESIGN.md`](07ASSETREGISTERDESIGN.md).
>
> Three rules below now carry Revision 3 weight:
> - **Rule 6 (filtered unique indexes as concurrency law)** gained
>   `UX_AssetHolding_AssetLocation` / `_AssetSite`, and the physical
>   verification one-per-cycle index **split in two** — unit rows are unique per
>   cycle, bulk rows unique per cycle *per place*.
> - `CK_AssetHolding_NonNegative` is the one **547** the API translates to a
>   **409** rather than treating as a coding bug: insufficient stock is a
>   user-facing race, not a defect.
> - `Compare-Schema.ps1` compares columns, indexes, foreign keys, CHECKs,
>   **DEFAULT constraints and sequences**. Both of the last two were added
>   after something shipped without them while the check said MATCH. If you
>   add a kind of object the script does not read, add it to the inventory
>   in the same change.
> - `AssetFinance` and `AssetDepreciationEntry` are **read-only in the API**.
>   They are written only by `SapSync` and `DataImport`, through the `Assets`
>   PublicApi write contract (rule 4a). AMS never computes depreciation.

## 1. Database rules (inherited from the design, restated for code)

1. One schema per module; a table lives in exactly one schema.
2. **No FKs across schemas.** Cross-module columns are ids only — in EF that means **no navigation property across modules**, ever.
3. Singular PascalCase tables, PK `Id`, FK `{Entity}Id`.
4. Instants are `datetime2` UTC named `*OnUtc`. Local wall-clock times are `time(0)` + the location's `TimeZoneId`. The API and EF never convert; the SLA service converts once at the edge.
5. Money/percentages are `decimal`, never float/double.
6. Concurrency-critical business rules are **filtered unique indexes** (one active allocation per asset, one pending approval instance, one default team…). Application code catches SqlException 2601/2627 and returns 409 — it does not pre-check with a read.
7. Concurrency tokens: `rowversion` on editable tables — **except the five system-versioned tables** (`Employee`, `Asset`, `Contract`, `SlaPolicy`, `LocationOperationalHour`), where SQL Server forbids rowversion. Those carry `ConcurrencyStamp uniqueidentifier` (R2-1 nominated `SysStartTime`; **R2-22 replaced it** — see §4).
7a. **`RowVersion` is never nullable.** Declare it `public byte[] RowVersion { get; set; } = [];` — never `byte[]?`. R2-14 made every remaining rowversion column NOT NULL because the value is always generated and a NULL stated a falsehood; a nullable CLR property silently produces a nullable column. An architecture test fails the build on `byte[]?`, and the schema-parity check catches it again downstream.
8. Booleans `Is/Has`; soft delete is `IsDeleted` — soft-deleted rows are filtered in queries, not by a global filter on modules where background jobs must see them.

## 2. DbContext per module

```csharp
public sealed class AllocationsDbContext(DbContextOptions<AllocationsDbContext> options) : DbContext(options)
{
    public DbSet<AssetAllocation> Allocations => Set<AssetAllocation>();
    public DbSet<AssetHandover>  Handovers   => Set<AssetHandover>();

    // The parameter MUST be named modelBuilder: CA1725 requires an override to
    // keep the base class's parameter names, and the build treats it as an error.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Allocations");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AllocationsDbContext).Assembly);
    }
}
```

- One DbContext per module, `HasDefaultSchema` set, configurations discovered from the module assembly only.
- All contexts share one connection string and one migrations history table **per module**, so modules migrate independently. The table is named `__EFMigrationsHistory` in every module and separated by **schema**, not by suffix: `MigrationsHistoryTable("__EFMigrationsHistory", "Allocations")` produces `[Allocations].[__EFMigrationsHistory]`.
- `SaveChanges` interceptors registered centrally: audit field interceptor (with the secrets exclusion set), `CreatedOnUtc/ModifiedOnUtc/CreatedBy/ModifiedBy` stamping interceptor.
- **All module contexts in a request share one `DbConnection`**, registered scoped, so a command that touches two modules commits atomically without MSDTC (01 rule 4a). Register the connection, not just the connection string, and build every context with `UseSqlServer(sharedConnection)`. `UnitOfWorkBehavior` calls `ctx.Database.UseTransaction(tx)` on each context it resolves. A context that opens its own connection breaks atomicity silently — the architecture test asserts every module registration takes the shared connection.

## 3. Entity configuration — one file per entity, mirrors the script

```csharp
public sealed class AssetHandoverConfiguration : IEntityTypeConfiguration<AssetHandover>
{
    public void Configure(EntityTypeBuilder<AssetHandover> e)
    {
        e.ToTable("AssetHandover", t =>
        {
            t.HasCheckConstraint("CK_AssetHandover_Status",
                "[Status] IN (N'HandedOver', N'InTransitToHo', N'ReceivedAtHo', N'Cancelled')");
            // …every CHECK from the script, verbatim
        });
        e.Property(x => x.Status).HasMaxLength(30).HasConversion<string>();
        e.Property(x => x.Remarks).HasMaxLength(500).IsRequired();
        e.Property(x => x.RowVersion).IsRowVersion();
        e.HasIndex(x => x.AssetId)
         .IsUnique()
         .HasFilter("[Status] = N'HandedOver'")
         .HasDatabaseName("UX_AssetHandover_OneOpenPerAsset");
        // FK only because AssetAllocation is the SAME schema:
        e.HasOne<AssetAllocation>().WithMany()
         .HasForeignKey(x => x.AllocationId).OnDelete(DeleteBehavior.NoAction);
        // AssetId, FromEmployeeId, BranchLocationId: plain int columns. NO navigation. NO FK.
    }
}
```

Standards:

- **Every** max length, CHECK, default, filtered index and FK from the script appears in configuration with the **same constraint/index name**. Diff `dotnet ef migrations script` output against the reference script per release.
- String enums map via conversion to the exact CHECK literals.
- Sequences: `b.HasSequence<long>("RequestNumberSequence", "ServiceDesk");` and number formatting happens in the handler, not the DB.
- Decimals: `HasPrecision(18, 2)` (money) / `(18, 4)` (custom values) / `(9, 6)` (GPS) / `(5, 2)` (health).

## 4. Temporal tables & concurrency (R2-1, amended by R2-22)

```csharp
builder.ToTable("Asset", table => table.IsTemporal(temporal =>
{
    temporal.HasPeriodStart("SysStartTime");
    temporal.HasPeriodEnd("SysEndTime");
    temporal.UseHistoryTable("AssetHistory", "Assets");
}));

// R2-22: the token is ConcurrencyStamp. The period columns are history, and
// NOTHING may map one as a concurrency token — an architecture test fails the
// build if anything does. See the measurement below for why.
builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();
```

- The five temporal tables have **no RowVersion property** — SQL Server forbids one — and carry `ConcurrencyStamp uniqueidentifier` instead. Everything else editable maps `IsRowVersion()`.
- The audit interceptor re-generates `ConcurrencyStamp` on every update, so the token changes because the *application* changed the row.
- Both tokens flow to the client as an opaque `etag` on responses; commands carry it back; mismatch → 412.
- History queries use `TemporalAsOf(...)` — the `history.view` capability gates the endpoints that expose it.

The rest of this section records why R2-1's original answer was replaced. It is kept because R2-22 is otherwise an assertion nobody can check.

### This gate FAILED. R2-1 needs revisiting.

Measured against SQL Server 2022 Express and EF Core 10.0.11, in `src/Backend/tests/AMS.PersistenceGates.Tests/GateA_TemporalConcurrency.cs`:

| | Question | Result |
|---|---|---|
| 1 | an update against a stale `SysStartTime` throws `DbUpdateConcurrencyException` | **only if the clock ticked** |
| 2 | an update against a fresh one succeeds | passes |
| 3 | after `SaveChanges`, the tracked `SysStartTime` is the value SQL Server wrote | passes |
| 4 | two updates to the same row inside one transaction both succeed | passes |

R2-1's premise is that `SysStartTime` "is regenerated on every UPDATE". **It is not.** SQL Server stamps the period start from the *transaction start time*, and the Windows system clock advances in ticks of roughly 1–15 ms. Two updates inside one tick receive the **same** `SysStartTime`.

Measured on the dev instance: **20 of 20** insert-then-update pairs left `SysStartTime` unchanged; inserting a 50 ms delay changed it every time.

The consequence is not a flaky test. It is a **silent lost update**: the second writer's stale token still matches, their `UPDATE` affects one row, no exception is raised, and the first writer's change is gone with nothing recorded anywhere. Two administrators editing the same asset in the same second is not an exotic scenario, and this is exactly the failure optimistic concurrency exists to prevent.

A second consequence follows: SQL Server does not keep a zero-duration history row, so back-to-back edits inside one tick leave **no history version** either — which matters for the `history.view` capability and "view a record as of a past date".

**Fixed in R2-22.** The five system-versioned tables now carry:

```sql
[ConcurrencyStamp] uniqueidentifier NOT NULL CONSTRAINT [DF_<Table>_ConcurrencyStamp] DEFAULT (NEWID())
```

and **that** is the concurrency token:

```csharp
builder.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();
```

The audit interceptor re-generates it on every update, so the token changes because the application changed the row — not because the clock happened to move. It costs 16 bytes a row.

`SysStartTime` goes back to doing only what it is actually for: history, and `TemporalAsOf` queries. **Do not map it as a concurrency token on any entity**; an architecture test fails the build if you do.

So the rule across the whole model is now uniform:

| Table kind | Token |
|---|---|
| ordinary editable table | `RowVersion` — `byte[]`, never `byte[]?` (rule 7a) |
| the five system-versioned tables | `ConcurrencyStamp` — `Guid` |

`GateA_TemporalConcurrency.cs` stays committed. It no longer guards a decision we rely on, but `Test1b` is the reproduction of why the decision changed, and deleting it would leave R2-22 as an assertion nobody can check.

## 5. Migrations discipline

- One migration per PR, named `{yyyyMMdd}_{Module}_{WhatChanged}`; never edit an applied migration.
- `dotnet ef migrations script --idempotent` output is the deployment artifact.
- **Schema parity is checked, not assumed**: `./build/Compare-Schema.ps1` builds two throwaway databases — one from a fresh run of `AMS_Consolidated_Design_v2.sql`, one from migrate-from-zero — and compares every column, index, filter, foreign key, delete rule and CHECK. It exits non-zero on any difference. Run it before opening a PR that touches a configuration, and add the new schema to `-Schemas` when a module gets its first migration.
  ```powershell
  ./build/Compare-Schema.ps1                          # default: Identity
  ./build/Compare-Schema.ps1 -Schemas Identity,Organization
  ```
  It earned its place on the first run, catching a `RowVersion` mapped nullable against R2-14's NOT NULL. That is the entire class of mistake it exists for: a difference too small to notice by reading and too important to ship.
- Seed/reference data (Section 17 of the script — statuses, regions, SLA policies, reminder windows, capabilities) is applied by an idempotent startup seeder that mirrors the script's guarded inserts. Seeds never overwrite admin edits.
- Renames use explicit `RenameColumn/RenameTable` — never drop-and-create for a table with data.

## 6. Query standards

- Reads: `AsNoTracking()` + `Select` projection into the Response type. No entity ever crosses a module or wire boundary.
- Writes: load the aggregate tracked, mutate via domain methods, one `SaveChangesAsync`.
- **No `FromSqlRaw` with interpolated user input**; parameterised always. Raw SQL is allowed only in `AMS.Reporting` and in SLA/calendar computations where set-based SQL beats LINQ — each raw query lives in its own file with a test.
- N+1 is a review-blocker; cross-schema "joins" in reporting go through views created in a dedicated `Reporting` migration set.
- Pagination hits the filtered indexes the script provides (e.g. the SLA queue reads `IX_ServiceRequest_SlaQueue`); if a new list screen needs an index, the index goes in the script AND a migration — not just one.

## 7. Error translation (the concurrency law)

| SQL | Meaning | HTTP |
|---|---|---|
| 2601 / 2627 on `UX_AssetAllocation_OneActivePerAsset` | someone else allocated first | 409 `Allocation.AlreadyActive` |
| 2601 / 2627 on `UX_RequestApprovalInstance_OnePending` | duplicate submission retry | 409 (return the existing run) |
| 2601 / 2627 on `UX_PhysicalVerification_ClientCapture` | the phone resent its own offline capture | **200** with the existing row — not an error |
| 2601 / 2627 on `UX_PhysicalVerification_OnePerAssetPerCycle` | another technician verified it first | 409 `Verification.AlreadyVerified`, naming who and when |
| 547 (CHECK) | invariant violated — usually a coding bug | 500 + alert, message never parroted to user |
| `DbUpdateConcurrencyException` | stale rowversion/SysStartTime | 412 |

A shared `SqlErrorTranslator` maps index name → stable error code; new filtered unique indexes must register a translation or the build's architecture test fails.

## 8. Data safety

- Encrypted-at-rest columns (`MfaSecretEncrypted`, `OsKeyEncrypted`, `LicenseKeyEncrypted`, `SmtpPasswordEncrypted`) are `byte[]` protected with ASP.NET Data Protection, purpose strings as commented in the script. They are excluded from audit, logging, and any `Select` that feeds a grid.
- Backups/restore and retention are ops concerns, but **hard DELETE of business rows is forbidden in code**: assets/contracts soft-delete; tickets close; approval history is undeletable by FK design (R2-12) — do not "fix" those FKs.
