# AMS — 07 Asset Register Design (Revision 3)

**Supersedes [`06ASSETMODELREVISION.md`](06ASSETMODELREVISION.md).**
All measurements are from `docs/Fujitec India- FAR as on 18-07-2026.xlsx`
(7,413 data rows), profiled 12 August 2026.

One register holding every asset the company owns — chair, chain pulley,
scaffold lot, software licence, asset-under-construction, laptop — each one row
with the same timeline, the same audit and the same concurrency law.

---

## 1. What the data forced

Doc 06 reasoned about the register. This one profiled it, and four of doc 06's
claims did not survive:

1. **TechnicalGroup is an independent axis, not a child of Category.**
   49 TechnicalGroups appear under more than one Asset Category and 86 under
   more than one Asset Class. `Storage Rack` is Furniture & Fixtures *and*
   Plant & Machinery *and* Office Equipments; `Test Meter` spans five classes.
   A category → technical-group tree would silently misclassify hundreds of
   rows on import.
2. **Asset Category is a pure function of Asset Class** — the 13 × 9 cross-tab
   has exactly 13 rows. Category is a *column* on the class, not a table.
3. **Itemisation is already the norm.** The 1,163 chairs are 1,163 rows, each
   individually numbered. Only **463 of 7,413 rows** carry Quantity > 1, and
   they are scaffolding (8,208 units on one row), barricades, bins, crates and
   pallets — pooled site material, never issued to a named person.
4. **Insurance is two policies covering 7,366 assets.** One Standard Fire &
   Special Perils policy alone covers 7,352. That is a contract covering many
   assets, which `Contracts.Contract` + `ContractAsset` already model.

Also: `AUCNo` is populated on **6,488** rows. Capitalisation from an
asset-under-construction is not an edge case — it is how most of this register
came into existence.

## 2. The core decision

**One `Assets.Asset` table. One row = one register line. A row-level `IsBulk`
mode. Optional 1:1 detail tables switched by taxonomy flags. No table-per-type,
no second register.**

- **The 24 % / 76 % split kills table-per-type.** Allocation, handover,
  movement, transfer, timeline, verification, contracts, custom fields, audit
  and SAP sync all key on `AssetId`. Table-per-type multiplies every one of
  those by the number of types — or forces a shared base table, which *is* the
  single table plus joins. The current design's mistake was never "one table";
  it was IT columns in the core. Fix the columns, keep the table.
- **The variance between types is behavioural, not structural.** A chair, a
  chain pulley and a laptop differ in which flows and detail tables apply, not
  in what an asset fundamentally is. Four narrow 1:1 details plus the existing
  custom-field mechanism absorb the rest.
- **Temporal versioning survives unchanged.** `AssetHistory` keeps answering
  "what did this record say on 31 March" for every asset the company owns.
  R2-22's `ConcurrencyStamp` carries over exactly.

## 3. Two taxonomies, one column

| FAR column | What it is | Modelled as | Owner |
|---|---|---|---|
| **Asset Class** (13) | how accounting depreciates and reports | `Assets.AssetClass` — new lookup | Finance, seeded from SAP |
| **Asset Category** (9) | a fixed roll-up of Asset Class | `AssetClass.ReportingCategory` — a **column** | Finance |
| **TechnicalGroup** (342) | what the thing physically is; drives behaviour, custom fields, screens | `Assets.AssetType` — **rename of the existing `AssetCategory` table** + behaviour flags | Asset admins |

Two independent axes on every asset: `Asset.AssetClassId` (finance, nullable
until classified) and `Asset.AssetTypeId` (operational, required). The
cross-product is unconstrained, because finding 1 proves it must be.

### Why rename `AssetCategory` → `AssetType`

The FAR uses "Asset Category" to mean the **accounting roll-up**. Keeping a
table of that name meaning "technical group" guarantees every conversation with
finance goes wrong. The application has not shipped; the rename costs one
migration. `AssetType.ParentAssetTypeId` survives for *operational* grouping
("IT > Laptops") — explicitly **not** the nine FAR categories.

Behaviour flags live on `AssetType` because custom fields already do, and
because "can a barricade be issued to a person" is an operational judgement,
not an accounting one.

## 4. The tables

Conventions: audit quartet abbreviated `…audit…`; every editable table carries
`RowVersion rowversion NOT NULL` except `Asset`, which is temporal and uses
`ConcurrencyStamp` (R2-22). Every FK shown is intra-schema; cross-module
columns are id-only (rule 2).

### 4.1 `Assets.AssetClass` — NEW, 13 rows

```sql
CREATE TABLE [Assets].[AssetClass] (
    [Id]                int           NOT NULL IDENTITY,
    [ClassCode]         nvarchar(20)  NOT NULL,   -- FAR "Asset Class Code"
    [ClassName]         nvarchar(100) NOT NULL,   -- Furniture & Fixtures, AUC, ...
    [ReportingCategory] nvarchar(100) NOT NULL,   -- FAR "Asset Category": the 9-way roll-up
    [IsDepreciable]     bit           NOT NULL,   -- Leasehold Land = 0
    [IsIntangible]      bit           NOT NULL,
    [IsAuc]             bit           NOT NULL,   -- exactly one row
    [IsActive]          bit           NOT NULL,
    …audit…, [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_AssetClass] PRIMARY KEY ([Id])
);
CREATE UNIQUE INDEX [UX_AssetClass_Code] ON [Assets].[AssetClass] ([ClassCode]);
CREATE UNIQUE INDEX [UX_AssetClass_Name] ON [Assets].[AssetClass] ([ClassName]);
```

No depreciation-method or chart-of-account **defaults** here. Under §8 those
are per-asset mirrors of what SAP posted, and a default nobody computes from is
documentation pretending to be schema.

### 4.2 `Assets.AssetType` — RENAMED, + behaviour flags

```sql
-- RenameTable AssetCategory -> AssetType
-- RenameColumn ParentCategoryId -> ParentAssetTypeId, CategoryName -> TypeName
ALTER TABLE [Assets].[AssetType] ADD
    [IsAllocatable]     bit NOT NULL CONSTRAINT [DF_AssetType_IsAllocatable]     DEFAULT (1),
    [IsPhysical]        bit NOT NULL CONSTRAINT [DF_AssetType_IsPhysical]        DEFAULT (1),  -- 0 = software/licence: no serial, no location, no verification
    [IsBulkDefault]     bit NOT NULL CONSTRAINT [DF_AssetType_IsBulkDefault]     DEFAULT (0),
    [TracksHardware]    bit NOT NULL CONSTRAINT [DF_AssetType_TracksHardware]    DEFAULT (0),
    [TracksSoftware]    bit NOT NULL CONSTRAINT [DF_AssetType_TracksSoftware]    DEFAULT (0),
    [TracksVehicle]     bit NOT NULL CONSTRAINT [DF_AssetType_TracksVehicle]     DEFAULT (0),
    [TracksCalibration] bit NOT NULL CONSTRAINT [DF_AssetType_TracksCalibration] DEFAULT (0);
```

`CustomFieldDefinition.AssetCategoryId` → `AssetTypeId`;
`UX_CustomFieldDefinition_CategoryField` → `UX_CustomFieldDefinition_TypeField`.

### 4.3 `Assets.Asset` — REVISED core

```sql
ALTER TABLE [Assets].[Asset] ADD
    [AssetTypeId]            int           NOT NULL,   -- rename of AssetCategoryId
    [AssetClassId]           int           NULL,       -- NULL until finance classifies
    [Make]                   nvarchar(100) NULL,       -- promoted from hardware detail: universal
    [Model]                  nvarchar(100) NULL,
    [IsBulk]                 bit           NOT NULL CONSTRAINT [DF_Asset_IsBulk] DEFAULT (0),
    [Quantity]               decimal(18,3) NOT NULL CONSTRAINT [DF_Asset_Quantity] DEFAULT (1),
    [UnitOfMeasure]          nvarchar(20)  NULL,
    [CapitalisedFromAssetId] int           NULL,       -- the AUC this settled from
    [SplitFromAssetId]       int           NULL;       -- the bulk line this was carved out of

ALTER TABLE [Assets].[Asset] DROP COLUMN [Hostname];             -- -> AssetHardwareDetail
ALTER TABLE [Assets].[Asset] DROP COLUMN [CalibrationStartDate]; -- -> AssetInstrumentDetail
ALTER TABLE [Assets].[Asset] DROP COLUMN [CalibrationEndDate];   -- (CK_Asset_CalibrationWindow moves with them)

ALTER TABLE [Assets].[Asset] ADD
    CONSTRAINT [CK_Asset_QuantityPositive]  CHECK ([Quantity] > 0),
    CONSTRAINT [CK_Asset_UnitQuantityIsOne] CHECK ([IsBulk] = 1 OR [Quantity] = 1),
    CONSTRAINT [CK_Asset_BulkHasUom]        CHECK ([IsBulk] = 0 OR [UnitOfMeasure] IS NOT NULL),
    CONSTRAINT [CK_Asset_BulkNotHeld]       CHECK ([IsBulk] = 0 OR ([CurrentEmployeeId] IS NULL AND [CurrentLocationId] IS NULL));
```

`CK_Asset_UnitQuantityIsOne` is the load-bearing one: it makes "every
allocatable asset has Quantity = 1" a **proof**, so allocation, handover and
unit verification never have to reason about quantity at all.
`CK_Asset_BulkNotHeld` forces bulk custody through `AssetHolding` — a bulk line
has no single current location because it is in four places at once.

`UX_Asset_Number` holds for the import: `Asset No` is unique across all 7,413
rows. `SerialNumber` stays non-unique (2,659 populated, vendor duplicates in
the wild).

### 4.4 `Assets.ChartOfAccount` — NEW lookup

Three code/description pairs per FAR row; normalising the pair stops 7,000
copies of one description drifting apart.

### 4.5 `Assets.AssetFinance` — NEW, 1:1, the SAP mirror

Holds original/migrated/additional/gross value, accumulated depreciation, net
book value, method, percent, useful life, capitalised quantity, first
acquisition and posting dates, `SapPostingStatus`, `AucReference` (the FAR's
`AUCNo`, present on 6,488 rows even where the AUC predates AMS),
`OpportunityName`, voucher and AP-voucher numbers, three CoA foreign keys, and
**`LastSyncedOnUtc`**.

Written **only** by SapSync and DataImport, through the Assets `PublicApi`
write contract (rule 4a). Read-only in the UI behind a `finance.view`
capability.

### 4.6 `Assets.AssetDepreciationEntry` — NEW, per asset per financial year

Opening accumulated, additions, charged for period, closing accumulated, net
book value at close, `SourceSystem` (`Sap` | `Import`), `SyncedOnUtc`.

```sql
CREATE UNIQUE INDEX [UX_AssetDepreciationEntry_AssetYear]
    ON [Assets].[AssetDepreciationEntry] ([AssetId], [FinancialYear]);
```

`ON DELETE NO ACTION` — financial evidence blocks deletion, same reasoning as
R2-12. The unique index makes a re-run of the yearly sync an upsert, never a
double count. **No `IsPosted`**: AMS does not post (§8).

### 4.7 `Assets.AssetHolding` — NEW, where bulk quantity actually lives

```sql
CREATE TABLE [Assets].[AssetHolding] (
    [Id]             int           NOT NULL IDENTITY,
    [AssetId]        int           NOT NULL,
    [LocationId]     int           NULL,   -- Organization.Location, id only
    [CustomerSiteId] int           NULL,   -- Allocations.CustomerSite, id only
    [OnHandQuantity] decimal(18,3) NOT NULL,
    …audit…, [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_AssetHolding] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_AssetHolding_NonNegative]  CHECK ([OnHandQuantity] >= 0),
    CONSTRAINT [CK_AssetHolding_OnePlaceKind] CHECK (([LocationId] IS NOT NULL AND [CustomerSiteId] IS NULL)
                                                  OR ([LocationId] IS NULL AND [CustomerSiteId] IS NOT NULL))
);
CREATE UNIQUE INDEX [UX_AssetHolding_AssetLocation] ON [Assets].[AssetHolding] ([AssetId], [LocationId])     WHERE [LocationId] IS NOT NULL;
CREATE UNIQUE INDEX [UX_AssetHolding_AssetSite]     ON [Assets].[AssetHolding] ([AssetId], [CustomerSiteId]) WHERE [CustomerSiteId] IS NOT NULL;
```

The filtered unique indexes make "one balance row per asset per place" a
database fact, so two concurrent receipts collide on 2601 and one retries as an
increment. `CK_AssetHolding_NonNegative` is the over-issue backstop.

### 4.8 Other new tables

`AssetDisposal` (partial disposal is real — 12 FAR rows have Disposal Qty > 0),
`AssetVehicleDetail` (registration unique, fitness/PUC/insurance expiry,
odometer), `AssetInstrumentDetail` (calibration window moved off the core, plus
agency, certificate, range, accuracy — 221 FAR rows carry calibration dates, so
the due-report reads one narrow index instead of scanning the register).

### 4.9 Deltas to existing tables

| Table | Change |
|---|---|
| `AssetHardwareDetail` | gains `Hostname`; loses `Make`/`Model` to the core |
| `AssetPurchaseDetail` | gains `GrnNumber`, `WarrantyMonths` |
| `AssetEvent` | gains `QuantityDelta`, `DisposalId` |
| `Movements.AssetMovement` | gains `Quantity` + positive CHECK |
| `Verification.PhysicalVerification` | gains `IsBulkCount`, `CountedQuantity`, `ExpectedQuantitySnapshot`; the one-per-cycle unique index **splits in two** (one filtered on unit rows, one on bulk-per-place) |
| `Contracts.Contract` | `Insurance` joins the contract-type vocabulary |
| `Allocations.CustomerSite` / `AssetSiteMapping` | gain `CustomerName` / `CommissionedDate` — this is where FieldAssets lands |

## 5. Bulk and quantity — the specific mechanism

**Rule 1 — a bulk line is never allocated to a person.** `CK_Asset_BulkNotHeld`
plus `AssetType.IsAllocatable = 0`. `UX_AssetAllocation_OneActivePerAsset`
stays exactly as reviewed and never reasons about quantity.
If somebody genuinely must pin 20 barricades on a supervisor, the answer is a
**split**: decrement the bulk line and its holding, create a new unit `Asset`
with `SplitFromAssetId` set, and allocate *that* like any laptop. One
mechanism, no dual-mode allocation table — and the FAR's own itemisation
practice says this is already how the company thinks.

**Rule 2 — bulk custody is place-level balances; movement moves quantity
between them.** Despatching 200 barricades writes one `AssetMovement` with
`Quantity = 200`, and in the same cross-module transaction the Assets write
contract runs a **set-based decrement**, so a concurrent over-issue dies on
`CK_AssetHolding_NonNegative` inside the database rather than in a
read-then-write check. While in transit the 200 belong to neither balance —
matching the reviewed design's "an asset in transit belongs to neither branch".
Invariant: `Asset.Quantity = SUM(holdings) + SUM(in-transit quantities)`.

**Rule 3 — verification counts a place, it does not scan a serial.** A bulk
capture is `IsBulkCount = 1` with a location, a counted quantity and a snapshot
of what was expected. The split unique indexes let a bulk asset be counted once
per place per cycle while unit assets keep the strict one-per-cycle rule and
the offline `ClientCaptureId` retry semantics (R2-21) unchanged.

## 6. AUC and intangibles

**AUC** is an ordinary `Asset` in the `IsAuc` class with status
`Under Construction`, accumulating cost in `AssetFinance.GrossValue` — and it
appears on the register, the timeline and verification, which the current
design cannot do for the 77 live AUC rows. **Capitalisation is a command, not
an UPDATE:** it creates the real asset rows with `CapitalisedFromAssetId` set
(one settlement can yield several), moves the AUC row to terminal status
`Capitalised`, and writes events to both timelines in one transaction. For the
6,488 rows whose AUC predates AMS, `AssetFinance.AucReference` carries the SAP
reference; neither column substitutes for the other.

**Intangibles** are assets whose `AssetType.IsPhysical = 0`: no serial, no QR,
no location, no holder, skipped by the verification-cycle builder. They keep
class, finance, amortisation schedule (same table — amortisation is
depreciation with a different name), contracts, custom fields and timeline. No
new table.

## 7. `FieldAssets` folds in — decided

The module, schema and table are deleted. Every column has a richer home:
`AssetNo` → `Asset.AssetNumber`; `CategoryName` → `AssetTypeId` (the importer
creates missing types); `SiteName`/`CustomerName` → `CustomerSite` +
`AssetSiteMapping`, which already has the one-active-site filtered index;
`CustodianEmployeeId` → an `AssetAllocation`; `CommissionedDate` →
`AssetSiteMapping`. The `field-asset.*` capabilities survive as a scoped
**view** of the one register rather than a gate on a second one.

The reviewed design's own argument against `FieldAssetAdmins` — "a second
identity store means a second password policy" — applies verbatim to a second
register.

## 8. SAP owns depreciation. AMS mirrors.

The evidence is one-directional: the FAR is an *export from* the accounting
system, every row uses one method, `Status` uses SAP vocabulary (`New`/`Post`),
and the schema already carries `SapAssetNumber`, `SapAssetClass`, `SapPlant`
and a whole SapSync module. Building a statutory depreciation engine alongside
a live ERP is how two numbers for one asset reach two reports.

`AssetFinance` and `AssetDepreciationEntry` are sync-written mirrors. No
calculation job, no period close, no posting flag.

**If it ever flips:** `AssetClass` gains method/percent/life and CoA defaults;
`AssetDepreciationEntry` gains `IsPosted`; a `DepreciationRun` header appears
with a one-active filtered unique index; a period lock appears. The table
*shapes* barely change — which is exactly why depreciation is a schedule and
not three columns. **Do not build any of it speculatively.**

## 9. Impact

Nothing has shipped, so "migration" means the design script, the EF model and
the dev migrations — no field data moves.

**Status as at 12 Aug 2026: items 1–5 and 7 are DONE and committed.** The
design script runs clean (15 schemas, 86 module tables, 94 with the approval
extension), `Compare-Schema.ps1` reports **1,665 objects, exact match**, 13
constraint probes pass and the suite is **186 passed, 0 failed**. Item 6
(`SqlErrorTranslator`) lands with the Assets slices, since it is a handler
concern and there are no Assets handlers yet. Item 8 (the FAR importer) is
`DataImport` work and has not started. Four defects found on the way in are
recorded in [`00DESIGNDECISIONS.md`](00DESIGNDECISIONS.md).

1. Design script → **Revision 3**: Section 3 rewritten, **Section 16 deleted**,
   deltas to Sections 4/5/9/10, Section 17 seeds (13 classes, `Under
   Construction`/`Capitalised`/`Disposed` statuses, `Insurance` contract type),
   module map **16 → 15 modules**, Assets **10 → 18 tables**. (This said 17
   when written. The list of additions in §4 has eight entries and 10 + 8 = 18;
   the `AssetCategory` → `AssetType` rename is not an addition. Corrected
   against the built database, which has 18.)
2. EF model: explicit `RenameTable`/`RenameColumn` (03 §5 — never drop-and-create
   a table with data), new entities and configurations, one delta migration each
   for Movements, Verification, Contracts, Allocations.
3. **`AMS.Modules.FieldAssets` deleted** — project, context, migrations, host
   registration, schema. This finally exercises 01 §1's "deleting a module must
   not break the build" for real.
4. `build/Compare-Schema.ps1`: drop `FieldAssets` from `-Schemas`. It will flag
   every rename, every new CHECK and the replaced verification index — that is
   it working.
5. `build/parse_design.py` regenerate; `build/assets_slices_taxonomy.py` reviewed
   against the rename.
6. **`SqlErrorTranslator`** registrations: both `AssetHolding` indexes, both new
   verification indexes, and a documented exception — `CK_AssetHolding_NonNegative`
   maps 547 → 409 `Stock.Insufficient`, because insufficient stock is a
   user-facing race, not the coding bug 03 §7 assumes a 547 to be.
7. Docs: `01` §1 module list; this document replaces `06`; the catalogue loses
   the FieldAssets module and gains the register screens.
8. DataImport: the FAR importer maps all 64 columns, creates missing
   `AssetType` rows, creates the two insurance contracts + 7,366 `ContractAsset`
   links, creates allocations for the 3,956 employee-assigned rows **flagged
   pre-acknowledged** (an import must not fire 3,956 acknowledgement e-mails),
   and creates holdings for the 463 bulk lines.

## 10. Open questions

1. **Will finance ever want AMS to *compute* depreciation**, or only display
   SAP's numbers? Decides whether §8's flip list is ever built.
2. **Are pool items ever issued to a named individual?** Occasional cases are
   covered by the split (§5); a *routine* issue/return counter would justify a
   per-employee issue ledger this design deliberately omits.
3. **Who issues asset numbers** for assets created in AMS before SAP knows
   them — AMS sequence with the SAP number back-filled (assumed), or must SAP
   number first?
4. **Does SapSync receive AUC settlement events**, or is capitalisation keyed
   manually from the settlement document? Same tables either way.
5. **Are the 24 Leasehold Building/Land rows in scope for verification cycles?**
   If not, that is another `IsPhysical = 0` decision for asset admins.
