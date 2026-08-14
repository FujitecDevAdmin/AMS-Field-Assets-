# AMS — 06 Asset Model Revision (proposal)

> # SUPERSEDED
>
> **Replaced by [`07ASSETREGISTERDESIGN.md`](07ASSETREGISTERDESIGN.md) on
> 2026-08-12.** A second pass profiled the FAR spreadsheet directly and
> **measured four of this document's claims to be wrong**. It is kept because
> the corrections only make sense beside what they correct:
>
> | This document said | The data says |
> |---|---|
> | §3.2 the category tree already models *Category → TechnicalGroup* | **49 TechnicalGroups appear under more than one Category, 86 under more than one Class.** `Storage Rack` is Furniture *and* Plant *and* Office Equipment. The tree would misclassify hundreds of rows on import. |
> | Three taxonomies are all real | **Category is a pure function of Class** (13 → 9, verified). Category needs no table — it is a column on `AssetClass`. |
> | §3.8 insurance needs a per-asset table | **7,366 insured rows are covered by two policies.** That is `Contracts.Contract` + `ContractAsset`, which already exist with reminder machinery. A per-asset table would store one policy number 7,352 times. |
> | §3.7 site deployment needs a new 1:1 table | `Allocations.CustomerSite` and `AssetSiteMapping` **already exist**, including the one-active-site filtered unique index. |
> | §5 Q2 "1,163 chairs are not 1,163 serial numbers" | **They are 1,163 rows.** Itemisation is already the norm; only **463 of 7,413 rows** have Quantity > 1, and they are scaffolding, barricades, bins and pallets — pooled site material, never issued to a named person. |
>
> The three decisions taken against this document still stand. See doc 07 §5.

The Assets module as designed holds office IT. The register the business
actually keeps holds everything the company owns, and IT is a quarter of it.
This document sets out what the tables need to become.

---

## 1. What the evidence says

`docs/Fujitec India- FAR as on 18-07-2026.xlsx` is the live fixed asset
register: **7,415 assets**, 64 columns, exported from the accounting system.

| Asset class | Rows | | Asset class | Rows |
|---|---:|---|---|---:|
| Furniture & Fixtures | 2,181 | | Plant & Machinery | 313 |
| Comp. h/w & s/w | 1,774 | | AUC | 77 |
| Office eqpt | 1,140 | | Intangible Asset | 60 |
| Installation eqpt | 983 | | Maintenance eqpt | 55 |
| Factory eqpt | 758 | | Canteen eqpt | 38 |
| | | | Leasehold bldg | 22 |
| | | | Vehicles | 10 |

**IT is 1,834 of 7,415 — 24%.** `Assets.Asset` carries `Hostname` on the core
row and hangs `AssetHardwareDetail` (processor, memory, MAC, IP) and
`AssetSoftwareDetail` (OS, Office, antivirus) off it. Those are correct for a
laptop and meaningless for 1,163 chairs, 118 barricades and 108 chain pulleys.

The register also carries three things the schema has **no** representation of:

- **Depreciation and book value** — method, percentage, useful life, opening
  and closing accumulated depreciation, depreciation charged for the year, net
  book value. Straight line, every asset.
- **Quantity** — `Capitalized Quantity`, `Disposal Qty`, `Gross Qty`. A row is
  not always one thing. Partial disposal is normal.
- **Chart of accounts** — three code/description pairs per asset: gross value,
  accumulated depreciation, depreciation charge.

Plus insurance policies, disposal records, vouchers, GRN and PO numbers, useful
life, and an `OpportunityName` linking an asset to the project it was bought
for.

## 2. What is wrong with the current shape

1. **IT specifics sit in the core.** `Asset.Hostname` and two IT-only detail
   tables. Every non-IT asset carries a column it can never use.
2. **One taxonomy doing three jobs.** `AssetCategory` is the only
   classification, but the business classifies three ways at once:
   *Asset Category* (9 values — how the register groups), *Asset Class*
   (13 values — how accounting depreciates), and *TechnicalGroup*
   (342 values — what the thing actually is: Chairs, Dell Laptop, Tool Kit).
3. **No finance dimension at all.** The register's entire reason for existing
   in accounting terms is absent.
4. **Every asset is assumed serialised and singular.** 1,163 chairs are not
   1,163 serial numbers.
5. **`FieldAssets` is a second, poorer register.** A flat table with its own
   `CategoryName` string, no allocation, no movements, no timeline, no custom
   fields, no contracts. Site equipment gets less than a laptop does.

## 3. The revision

Three concerns, kept apart:

| Concern | Answers | Tables |
|---|---|---|
| **Identity and taxonomy** | what is this thing | `AssetClass`, `AssetCategory`, `Asset` |
| **Custody and movement** | where is it, who has it | existing — Allocations, Movements, Verification |
| **Finance** | what is it worth, how does it depreciate | **all new** |

### 3.1 `Assets.AssetClass` — NEW

The accounting classification. Thirteen rows, maintained by finance, and the
thing depreciation reports group by.

```sql
CREATE TABLE [Assets].[AssetClass] (
    [Id]                          int            NOT NULL IDENTITY,
    [ClassCode]                   nvarchar(20)   NOT NULL,   -- "Asset Class Code" in the FAR
    [ClassName]                   nvarchar(100)  NOT NULL,   -- Furniture & Fixtures, Vehicles, AUC
    [DefaultDepreciationMethod]   nvarchar(30)   NOT NULL,   -- CHECK: StraightLine | WrittenDownValue | None
    [DefaultDepreciationPercent]  decimal(9,4)   NULL,
    [DefaultUsefulLifeMonths]     int            NULL,
    [IsDepreciable]               bit            NOT NULL,   -- Leasehold Land is not
    [IsIntangible]                bit            NOT NULL,   -- no serial, no physical check
    -- Chart of accounts. Defaults; an asset may override.
    [GrossValueCoaCode]           nvarchar(30)   NULL,
    [AccumulatedDepreciationCoa]  nvarchar(30)   NULL,
    [DepreciationChargeCoa]       nvarchar(30)   NULL,
    [IsActive]                    bit            NOT NULL,
    ... audit ...,
    CONSTRAINT [PK_AssetClass] PRIMARY KEY ([Id])
);
CREATE UNIQUE INDEX [UX_AssetClass_Code] ON [Assets].[AssetClass] ([ClassCode]);
```

### 3.2 `Assets.AssetCategory` — REVISED

Keep the self-referencing tree; it already models *Asset Category → Technical
Group* (9 roots, 342 leaves). Add the **behaviour flags** that tell the
application what an asset of this kind can do. These are what stop a chair
being allocated to an employee or a laptop needing a calibration certificate.

```sql
ALTER TABLE [Assets].[AssetCategory] ADD
    [IsSerialised]        bit NOT NULL CONSTRAINT [DF_AssetCategory_IsSerialised] DEFAULT (1),
    [IsAllocatable]       bit NOT NULL CONSTRAINT [DF_AssetCategory_IsAllocatable] DEFAULT (1),
    [TracksHardware]      bit NOT NULL CONSTRAINT [DF_AssetCategory_TracksHardware] DEFAULT (0),
    [TracksSoftware]      bit NOT NULL CONSTRAINT [DF_AssetCategory_TracksSoftware] DEFAULT (0),
    [TracksVehicle]       bit NOT NULL CONSTRAINT [DF_AssetCategory_TracksVehicle] DEFAULT (0),
    [TracksCalibration]   bit NOT NULL CONSTRAINT [DF_AssetCategory_TracksCalibration] DEFAULT (0),
    [TracksSiteDeployment] bit NOT NULL CONSTRAINT [DF_AssetCategory_TracksSite] DEFAULT (0);
```

**Why flags on the category and not a third taxonomy.** A separate `AssetType`
table was considered and rejected: three classifications is one more than
anybody will keep consistent. Custom fields already hang off the category, so
behaviour belongs there too.

### 3.3 `Assets.Asset` — REVISED core

```sql
ALTER TABLE [Assets].[Asset] ADD
    [AssetClassId]      int            NULL,      -- FK AssetClass; NULL until finance classifies it
    [ParentAssetId]     int            NULL,      -- FK Asset; a component, or the AUC it came from
    [Make]              nvarchar(100)  NULL,      -- PROMOTED from AssetHardwareDetail: universal
    [Model]             nvarchar(100)  NULL,      -- PROMOTED: a chair has a model too
    [Quantity]          decimal(18,4)  NOT NULL CONSTRAINT [DF_Asset_Quantity] DEFAULT (1),
    [Uom]               nvarchar(20)   NULL,      -- Nos, Set, Metre
    [TechnicalGroup]    nvarchar(100)  NULL;      -- as imported, until mapped to a category leaf

ALTER TABLE [Assets].[Asset] DROP COLUMN [Hostname];              -- moves to AssetHardwareDetail
ALTER TABLE [Assets].[Asset] DROP COLUMN [CalibrationStartDate];  -- moves to AssetInstrumentDetail
ALTER TABLE [Assets].[Asset] DROP COLUMN [CalibrationEndDate];    -- moves to AssetInstrumentDetail
```

`Quantity` is the change with the widest blast radius, and it is unavoidable:
`Capitalized Quantity` and `Disposal Qty` are columns in the live register.
Allocation, movement and verification all currently assume one row is one
thing. See §5.

### 3.4 `Assets.AssetFinance` — NEW, one row per asset

```sql
CREATE TABLE [Assets].[AssetFinance] (
    [AssetId]                     int            NOT NULL,   -- PK and FK, 1:1
    [OriginalValue]               decimal(18,2)  NULL,
    [MigratedBookValue]           decimal(18,2)  NULL,
    [AdditionalValue]             decimal(18,2)  NULL,
    [GrossValue]                  decimal(18,2)  NULL,
    [DepreciationMethod]          nvarchar(30)   NULL,       -- overrides the class default
    [DepreciationPercent]         decimal(9,4)   NULL,
    [UsefulLifeMonths]            int            NULL,
    [AccumulatedDepreciation]     decimal(18,2)  NULL,
    [NetBookValue]                decimal(18,2)  NULL,
    [CapitalisedQuantity]         decimal(18,4)  NULL,
    [FirstAcquisitionDate]        date           NULL,
    [PostingDate]                 date           NULL,
    [InvoiceNo]                   nvarchar(60)   NULL,
    [InvoiceDate]                 date           NULL,
    [PurchaseOrderNo]             nvarchar(60)   NULL,
    [GrnNumber]                   nvarchar(60)   NULL,
    [VoucherNo]                   nvarchar(60)   NULL,
    [ApVoucherNo]                 nvarchar(60)   NULL,
    [OpportunityName]             nvarchar(200)  NULL,       -- the project it was bought for
    [GrossValueCoaCode]           nvarchar(30)   NULL,       -- override the class defaults
    [AccumulatedDepreciationCoa]  nvarchar(30)   NULL,
    [DepreciationChargeCoa]       nvarchar(30)   NULL,
    ... audit, RowVersion ...,
    CONSTRAINT [PK_AssetFinance] PRIMARY KEY ([AssetId]),
    CONSTRAINT [FK_AssetFinance_Asset] FOREIGN KEY ([AssetId])
        REFERENCES [Assets].[Asset] ([Id]) ON DELETE CASCADE
);
```

Separate from `Asset` on purpose. Finance edits it, IT does not, and the two
have different capabilities and different audit expectations.

### 3.5 `Assets.AssetDepreciationEntry` — NEW, one row per asset per period

The FAR's *"Acc. Dep. as of beginning of Year / Depreciation Charged for the
year / Acc. Dep. as of End of Year"* is a schedule, not three columns. Storing
it as a schedule is what lets the register reproduce any prior year.

```sql
CREATE TABLE [Assets].[AssetDepreciationEntry] (
    [Id]                        bigint         NOT NULL IDENTITY,
    [AssetId]                   int            NOT NULL,
    [FinancialYear]             smallint       NOT NULL,   -- 2026 = FY 2026-27
    [OpeningAccumulated]        decimal(18,2)  NOT NULL,
    [ChargedForPeriod]          decimal(18,2)  NOT NULL,
    [ClosingAccumulated]        decimal(18,2)  NOT NULL,
    [NetBookValueAtClose]       decimal(18,2)  NOT NULL,
    [CalculatedOnUtc]           datetime2      NOT NULL,
    [IsPosted]                  bit            NOT NULL,   -- posted to the ledger
    CONSTRAINT [PK_AssetDepreciationEntry] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AssetDepreciationEntry_Asset] FOREIGN KEY ([AssetId])
        REFERENCES [Assets].[Asset] ([Id]) ON DELETE CASCADE
);
-- One run per asset per year, and a re-run cannot silently double-count.
CREATE UNIQUE INDEX [UX_AssetDepreciationEntry_AssetYear]
    ON [Assets].[AssetDepreciationEntry] ([AssetId], [FinancialYear]);
```

### 3.6 `Assets.AssetDisposal` — NEW

```sql
CREATE TABLE [Assets].[AssetDisposal] (
    [Id]                  int            NOT NULL IDENTITY,
    [AssetId]             int            NOT NULL,
    [DisposalDate]        date           NOT NULL,
    [DisposalQuantity]    decimal(18,4)  NOT NULL,   -- partial disposal is normal
    [DisposalGrossValue]  decimal(18,2)  NULL,
    [SaleProceeds]        decimal(18,2)  NULL,
    [DisposalReason]      nvarchar(300)  NOT NULL,
    [ApprovedByUserId]    int            NULL,
    ... audit ...,
    CONSTRAINT [PK_AssetDisposal] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_AssetDisposal_Quantity] CHECK ([DisposalQuantity] > 0),
    CONSTRAINT [FK_AssetDisposal_Asset] FOREIGN KEY ([AssetId])
        REFERENCES [Assets].[Asset] ([Id]) ON DELETE NO ACTION
);
```

A table, not a `DisposedOnUtc` column, because an asset with a quantity can be
disposed of in parts and each part is its own event with its own approval.

### 3.7 Optional 1:1 detail tables, applied by category flag

| Table | Applies when | Holds |
|---|---|---|
| `AssetHardwareDetail` *(exists)* | `TracksHardware` | processor, memory, storage, MAC, IP, **+ `Hostname` moved here** |
| `AssetSoftwareDetail` *(exists)* | `TracksSoftware` | OS, build, Office, antivirus, encrypted key |
| `AssetVehicleDetail` **NEW** | `TracksVehicle` | registration no, chassis no, engine no, fuel type, fitness expiry, PUC expiry, odometer |
| `AssetInstrumentDetail` **NEW** | `TracksCalibration` | **calibration start/end moved here**, agency, certificate no, range, accuracy class |
| `AssetSiteDeployment` **NEW** | `TracksSiteDeployment` | site name, customer name, opportunity, commissioned date, custodian employee id |

**The rule for detail table versus custom field:** a detail table when the
application has logic or a report that depends on it — insurance expiry drives
a reminder, calibration expiry drives a report. A custom field for everything
else. The custom-field mechanism already exists and already stores typed
values; it should absorb the long tail, not the things the system acts on.

### 3.8 `Assets.AssetInsurance` — NEW

The FAR carries policy number, type, start and end per asset. It behaves
exactly like a contract reminder and should reuse that pattern rather than
invent a second one.

### 3.9 `FieldAssets` — ABSORBED

`FieldAssets.FieldAsset` becomes an `AssetCategory` with
`TracksSiteDeployment = 1` and an `AssetSiteDeployment` row. It gains
allocation, movements, the timeline, verification, contracts and custom fields
— everything it does not have today. The `field-asset.*` capabilities stay, now
scoping a view of the one register rather than gating a second one.

**This deprecates a module.** `docs/01` §1 and the catalogue both list
`FieldAssets` as one of sixteen; that becomes fifteen plus a category.

## 4. What this costs

| | |
|---|---|
| New tables | 7 — `AssetClass`, `AssetFinance`, `AssetDepreciationEntry`, `AssetDisposal`, `AssetVehicleDetail`, `AssetInstrumentDetail`, `AssetSiteDeployment`, `AssetInsurance` |
| Changed tables | `Asset` (+7 columns, −3), `AssetCategory` (+7 flags) |
| Removed | `FieldAssets` schema and its module project |
| Documents | `01` module map, `03` §1, the catalogue's module and feature lists, the design script's Section 3 and 16 |
| Rework | the Assets slices built so far are unaffected; the register slices have not been written yet, which is why this is the moment |

The schema-parity check (`build/Compare-Schema.ps1`) is what makes this safe to
attempt: the design script and the EF model are compared object by object, so a
revision this size cannot drift silently.

## 5. Decisions — TAKEN

Recorded in full in [`00DESIGNDECISIONS.md`](00DESIGNDECISIONS.md).

| | Decision | What it changes here |
|---|---|---|
| 1 | **SAP owns depreciation; AMS mirrors it read-only** | `AssetFinance` and `AssetDepreciationEntry` stay, but are written only by SapSync. No depreciation run, no posting lifecycle. Both tables gain `LastSyncedOnUtc`; every finance field is read-only in the API. |
| 2 | **Hybrid quantity: serialised = one row, bulk = one line with a quantity** | `AssetCategory.IsSerialised` decides. Allocation, handover and physical verification apply **only** to serialised rows; bulk rows move by quantity. |
| 3 | **`FieldAssets` folds in** | Module and schema removed. Site equipment becomes a category with `TracksSiteDeployment = 1` and an `AssetSiteDeployment` row. Sixteen modules become fifteen. |

### What decision 2 forces, and it is not small

*An allocation may only reference a serialised asset* is now a rule of the
system, and **no foreign key can enforce it**: `IsSerialised` lives on
`AssetCategory` in `[Assets]`, and `AssetAllocation` lives in `[Allocations]`,
where design rule 2 forbids a cross-schema FK.

The likely answer is a denormalised `Asset.IsSerialised`, maintained from the
category on write, so that Allocations has something in its own reach to check
— but "check" here must mean a database constraint, not a handler `if`, or it
is a read-then-write race exactly like the ones rule 6 exists to prevent. This
is the piece of the revision most likely to be got wrong, and it should be
settled before a single Allocations slice is written.

### Still open, but not blocking

- **AUC (77 rows) → capitalisation.** A lifecycle transition, not just
  `ParentAssetId`. Can be designed after the core lands.
- **Intangibles (60 rows).** No serial, no location, no physical verification.
  Falls out of decision 2 naturally — they are non-serialised — but the
  verification module must skip them rather than report them missing.
