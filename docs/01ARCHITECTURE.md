# AMS — 01 Architecture

**Stack:** MSSQL Server 2022 · .NET 10 · EF Core · Minimal API · Angular 20 + DevExtreme 26.x · Flutter (asset audits)
**Style:** Modular monolith · DDD · CQRS · Feature-based vertical slices · Schema-per-module
**Source of truth for the data model:** `AMS_Consolidated_Design_v2.sql` (Revision 3). The EF model must produce exactly that design.

---

> **REVISION 3 HAS LANDED — the asset model is wider than office IT.**
> The live register holds 7,413 assets and IT is 24%. Design:
> [`07ASSETREGISTERDESIGN.md`](07ASSETREGISTERDESIGN.md), which supersedes
> doc 06. What changed: SAP owns depreciation and AMS mirrors it read-only;
> bulk quantity is a row-level mode with place-level holdings in
> `AssetHolding`; `FieldAssets` folded in, so **fifteen modules, not sixteen**;
> and `AssetCategory` is **renamed `AssetType`** because the FAR uses "Asset
> Category" to mean the accounting roll-up — which is now `AssetClass`.
> Proven by `build/Compare-Schema.ps1` at **1,665 objects, exact match**.

## 1. The shape of the system

```
ams/
├─ src/
│  ├─ AMS.Api/                        # ONE host. Minimal API. Wires all modules.
│  │   ├─ Program.cs
│  │   └─ appsettings.json
│  ├─ AMS.SharedKernel/               # Result<T>, Error, IDispatcher, base types, NO business logic
│  ├─ AMS.SharedKernel.Web/           # ToHttpResult, RequireCapability. HTTP only, no business logic.
│  │                                  #   Modules MAY reference this one — see docs/00DESIGNDECISIONS.md
│  ├─ AMS.Infrastructure/             # Cross-cutting: auth, outbox drain, file storage, clock
│  ├─ Modules/
│  │  ├─ AMS.Modules.Identity/
│  │  ├─ AMS.Modules.Organization/
│  │  ├─ AMS.Modules.Assets/
│  │  ├─ AMS.Modules.Allocations/
│  │  ├─ AMS.Modules.Movements/
│  │  ├─ AMS.Modules.Transfers/
│  │  ├─ AMS.Modules.ServiceDesk/     # includes the approval workflow
│  │  ├─ AMS.Modules.ServiceLevel/
│  │  ├─ AMS.Modules.Contracts/
│  │  ├─ AMS.Modules.Verification/    # also serves the Flutter audit app
│  │  ├─ AMS.Modules.Discovery/
│  │  ├─ AMS.Modules.SapSync/
│  │  ├─ AMS.Modules.Notifications/
│  │  ├─ AMS.Modules.Audit/
│  │  └─ AMS.Modules.DataImport/
│  └─ AMS.Reporting/                  # read-only queries across modules (reports/dashboards)
├─ web/                               # Angular + DevExtreme 26.x (see 04)
├─ mobile/                            # Flutter audit app (see 05)
└─ docs/
```

One deployable API, fifteen module projects. A module maps 1:1 to a database schema.
`FieldAssets` was the sixteenth until Revision 3 folded it into `Assets`.

A module that publishes a cross-module contract also has an
`AMS.Modules.<Name>.PublicApi` assembly beside it — the interface and its
DTOs, referencing `AMS.SharedKernel` and nothing else. **A module may
reference another module's `.PublicApi`; it may never reference the module.**
That is what makes rule 3 possible at all, and both halves are enforced by
`ModuleBoundaryTests`. One assembly per publishing module, not one shared
contracts project — see `00DESIGNDECISIONS.md` for why.
**Deleting a module project must not break the build of any other module** — that is the test of the boundary.

## 2. Module rules (non-negotiable)

1. **Schema per module.** `AMS.Modules.Assets` owns schema `[Assets]` and nothing else.
2. **No cross-module references.** No project reference from one module to another module's *implementation*. No EF navigation across schemas. Cross-module links hold the **id only** — exactly as the SQL design does (rule 2 of the schema). Referencing another module's `.PublicApi` assembly is permitted, and is how rule 3 is obeyed.
3. **Modules talk through contracts.** A module that publishes one ships an `AMS.Modules.<Name>.PublicApi` **assembly** of interfaces + DTOs, implemented in the module and registered in DI (e.g. `IAssetTimeline.AppendAsync(...)`). Consumers reference that assembly, never the module. A `PublicApi/` *folder* inside a module is not referenceable by anyone and was the reason this rule could not be obeyed until 12 Aug 2026.
4. **Same-transaction side effects use the timeline/outbox tables**, not events over the network. An `AssetEvent` or `EmailOutbox` row commits or rolls back with the change it describes (schema Section 3 / 13 rationale) — see rule 4a for how, because those tables belong to other modules.
4a. **The transaction spans modules; the DbContext never does.** *(Proven — `src/Backend/tests/AMS.PersistenceGates.Tests/GateB_CrossModuleTransaction.cs`: two module contexts on one connection commit together, roll back together when the second fails, and no MSDTC is involved.)* `AssetEvent` lives in `[Assets]` and `EmailOutbox` in `[Notifications]`, so a handler in Allocations cannot write either through its own context — one DbContext maps one schema (03 §2). Instead: every module context is built on **one shared `DbConnection`**, and `UnitOfWorkBehavior` opens **one transaction per command** and enlists each context the handler touches (`ctx.Database.UseTransaction(tx)`). The handler reaches the other module only through a **write contract** on that module's `PublicApi` — `IAssetTimeline.Append(...)`, `IEmailOutbox.Queue(...)` — whose implementation lives in the owning module and uses the owning module's context. Rule 2 holds (no project reference, no navigation, id only); atomicity holds; nobody writes another module's tables.
5. **Reporting is read-only.** `AMS.Reporting` may query views across schemas but never writes and never holds business rules.
6. **Authorisation is by capability**, never role name (`identity` module resolves the effective capability set; endpoints declare `.RequireCapability("handover.record")`).

## 3. Feature-based vertical slices (CQRS)

Every feature is a folder. Every file in the slice is separate — **one type per file, no shared "God" DTOs**:

```
AMS.Modules.Allocations/
├─ Domain/                            # entities, value objects, domain rules
│  ├─ AssetAllocation.cs
│  ├─ AssetHandover.cs
│  └─ ReturnCondition.cs              # value object over the CHECK list
├─ PublicApi/
│  └─ IAllocationLookup.cs
├─ Persistence/
│  ├─ AllocationsDbContext.cs
│  └─ Configurations/                 # one IEntityTypeConfiguration per entity
│     ├─ AssetAllocationConfiguration.cs
│     └─ AssetHandoverConfiguration.cs
└─ Features/
   ├─ AllocateAsset/                          # a COMMAND slice
   │  ├─ AllocateAssetCommand.cs              # the command (record)
   │  ├─ AllocateAssetRequest.cs              # wire-in DTO (bound from HTTP)
   │  ├─ AllocateAssetResponse.cs             # wire-out DTO
   │  ├─ AllocateAssetValidator.cs            # FluentValidation, validates the Request
   │  ├─ AllocateAssetHandler.cs              # the only place the work happens
   │  ├─ AllocateAssetMapper.cs               # Request→Command, Domain→Response
   │  └─ AllocateAssetEndpoint.cs             # Minimal API MapPost, one route
   ├─ HandoverToBranchStore/
   │  ├─ HandoverToBranchStoreCommand.cs
   │  ├─ HandoverToBranchStoreRequest.cs
   │  ├─ HandoverToBranchStoreResponse.cs
   │  ├─ HandoverToBranchStoreValidator.cs
   │  ├─ HandoverToBranchStoreHandler.cs
   │  ├─ HandoverToBranchStoreMapper.cs
   │  └─ HandoverToBranchStoreEndpoint.cs
   └─ GetMyAssets/                            # a QUERY slice
      ├─ GetMyAssetsQuery.cs
      ├─ GetMyAssetsRequest.cs
      ├─ GetMyAssetsResponse.cs
      ├─ GetMyAssetsValidator.cs
      ├─ GetMyAssetsHandler.cs
      ├─ GetMyAssetsMapper.cs
      └─ GetMyAssetsEndpoint.cs
```

**Rules of the slice**

- `*Command.cs` / `*Query.cs` — an immutable `record`; commands mutate, queries never do. A slice is one or the other, never both.
- `*Mapper.cs` — static `ToCommand`/`ToQuery` and `ToResponse` methods. Every slice has one, even where the mapping is a one-liner; it is the only place shapes are converted (02 §4).
- `*Request.cs` / `*Response.cs` — HTTP wire shapes only. Never expose a domain entity on the wire; never accept one from the wire.
- `*Validator.cs` — `AbstractValidator<TRequest>`. Shape/format checks only; **business invariants live in the domain/database** (e.g. one-holder-at-a-time is the filtered unique index, not a validator).
- `*Handler.cs` — `IRequestHandler<TCommand, Result<TResponse>>`. One handler per slice. The handler is the transaction boundary.
- `*Endpoint.cs` — Minimal API registration only: route, auth capability, versioning, `Results<Ok<...>, ValidationProblem, Conflict>` typed results. **No logic in endpoints.**
- A slice may not reference another slice's files. Shared logic moves DOWN into `Domain/`, never sideways.

## 4. Request pipeline

```
HTTP → Endpoint → Validate(Request) → Map → Dispatcher
     → [Logging → Capability check → UnitOfWork] → Handler → Result<T> → HTTP result
```

- Dispatcher: MediatR **or** a thin source-generated dispatcher — pick once, use everywhere.
- Pipeline behaviors (order matters): `LoggingBehavior` → `ValidationBehavior` → `AuthorizationBehavior` → `UnitOfWorkBehavior` (commands only).
- `UnitOfWorkBehavior` owns the transaction, not the handler: open on the shared connection before the handler runs, commit after it returns success, roll back on failure or on a failed `Result`. It enlists each module context resolved during the request (rule 4a). Queries are not wrapped.
- **Result pattern, not exceptions**, for expected failures: `Result<T>` with typed `Error(Code, Message)`. Exceptions are for bugs.
- Unique-index violations 2601/2627 are translated by the UnitOfWork behavior into `Error.Conflict` → HTTP 409 with a readable message (the schema is the concurrency law; the API translates it).
- `rowversion`/`SysStartTime` mismatches → HTTP 412 Precondition Failed.

## 5. API conventions

- Route shape: `/api/v1/{module}/{resource}` — e.g. `POST /api/v1/allocations/handovers`, `GET /api/v1/service-desk/tickets/{id}`.
- Endpoints grouped per module with `MapGroup("/api/v1/allocations").RequireAuthorization()`; each module contributes an `IModule.MapEndpoints(IEndpointRouteBuilder)` discovered at startup.
- Verbs: POST = command that creates, PUT = full replace, PATCH = never (use explicit commands: `POST .../tickets/{id}/hold`), DELETE only where the catalogue says delete exists (soft delete).
- Every list endpoint supports the **DevExtreme load contract** (skip/take/sort/filter/group) via a shared `DataSourceLoad` binding in `SharedKernel` — grids stay server-side.
- Idempotency: commands that can be retried carry a client GUID (`ClientDecisionId` pattern from the approval design).
- Time: API speaks **UTC ISO-8601 only**. Wall-clock branch times are `HH:mm` strings + the branch `TimeZoneId`, exactly as stored.

## 6. Background work

Hosted services in `AMS.Infrastructure`, one per job, each idempotent because the **database** makes it so (unique indexes, watermarks):
SLA minute monitor · scheduled-intake opener · escalation firer · contract reminder daily job · scheduled-field-change applier · email outbox drainer · SAP sync · stale-agent alert.
Jobs read configuration from tables, never from code (schema Section 8/9 rationale).

## 7. Testing strategy

- **Domain**: pure unit tests, no database.
- **Handlers**: integration tests against a real SQL Server (the local `.\SQLEXPRESS2022` instance), each fixture creating and dropping its own database from the module's migrations — filtered-index and CHECK behaviour is part of the spec, so mocks are forbidden here. This said "Testcontainers" until Revision 3; the suite has run against SQL Express since the spike.
- **Contract tests** per module PublicApi.
- **Architecture tests** (NetArchTest): no module→module reference, no domain→persistence reference, and every slice folder holds exactly seven files drawn from the eight allowed suffixes — `Command` **xor** `Query`, then `Request`, `Response`, `Validator`, `Handler`, `Mapper`, `Endpoint`.

## 8. The documents

| Doc | Owns |
|---|---|
| `docs/01ARCHITECTURE.md` | this file — boundaries, slices, pipeline |
| `docs/02BACKENDCODINGSTANDARDS.md` | C#/.NET 10 rules, naming, Result, validation, DI |
| `docs/03DATABASEEFCORESTANDARDS.md` | MSSQL + EF Core mapping of Revision 3, migrations, concurrency |
| `docs/04FRONTENDANGULARDEVEXTREME.md` | Angular + DevExtreme 26.x structure and rules |
| `docs/05FLUTTERMOBILEAUDIT.md` | the offline-first Flutter audit app |
| `docs/06ASSETMODELREVISION.md` | superseded by 07; kept for the corrections |
| `docs/07ASSETREGISTERDESIGN.md` | **Revision 3** — one register for every asset the company owns |
| `docs/00DESIGNDECISIONS.md` | every deviation from the reviewed design, newest first |

A rule stated in one document is not repeated in another; the more specific document wins on conflict, and this one wins on boundaries.
