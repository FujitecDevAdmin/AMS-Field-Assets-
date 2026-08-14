# AMS — instructions for anyone writing code here

This file is the entry point for **every** contributor, human or tool. Codex,
Claude Code, Copilot, Cursor and a developer with no assistant at all are held
to the same rules, because the repository cannot tell which one produced a
pull request and should not have to.

Read this file first. It is a map and a set of hard rules, not a substitute
for `docs/` — when you are about to work on something, open the document that
owns it.

---

## 1. What this is

An Asset Management System for Fujitec India: asset register, allocation and
return, movements between branches, a service desk with SLA, contracts,
physical verification on a phone, discovery agents, SAP sync.

**Modular monolith.** One deployable API, fifteen modules, one database,
fifteen schemas. A module maps 1:1 to a schema and owns it alone.

A module may reference another module's `.PublicApi` assembly. It may never
reference the module itself, and the architecture tests read `.csproj` files
to make sure.

| Layer | Where | Stack |
|---|---|---|
| API | `src/Backend/` | .NET 10, Minimal API, CQRS vertical slices, EF Core |
| Web | `src/Web/` | Angular + DevExtreme 26.1.x |
| Mobile | `src/Mobile/` | Flutter, offline-first audit app |
| Design | `AMS_Consolidated_Design_v2.sql` | MSSQL 2022, the reviewed target schema |

## 2. The documents, and which one owns what

Do not guess a convention. One of these already decided it.

| Document | Owns |
|---|---|
| `docs/00DESIGNDECISIONS.md` | every deviation from the reviewed design, why it was made, and the evidence. **Read this before concluding something is a bug.** |
| `docs/01ARCHITECTURE.md` | module boundaries, vertical slices, the request pipeline. **Wins on conflict about boundaries.** |
| `docs/02BACKENDCODINGSTANDARDS.md` | C#, naming, `Result<T>`, validation, endpoints, DI |
| `docs/03DATABASEEFCORESTANDARDS.md` | EF Core mapping, migrations, concurrency, query rules |
| `docs/04FRONTENDANGULARDEVEXTREME.md` | Angular structure, DevExtreme rules, HTTP layer |
| `docs/05FLUTTERMOBILEAUDIT.md` | the offline audit app, sync, idempotency |
| `docs/07ASSETREGISTERDESIGN.md` | **Revision 3, landed** — one register for every asset. Read before touching `[Assets]`. |
| `docs/06ASSETMODELREVISION.md` | superseded by 07; kept because the corrections only make sense beside what they correct |
| `docs/AMS_Module_Screen_Feature_Catalogue_v2.docx` | every screen and feature, by actor. The scope of the product. |

A rule stated in one document is not repeated in another. The more specific
document wins, except on boundaries, where 01 wins.

> **Revision 3 has LANDED.** The live register is 76% non-IT, so `[Assets]`
> now holds every asset the company owns — 18 tables, not 10.
> `AssetCategory` is **`AssetType`** and carries seven behaviour flags;
> `AssetClass` is the separate finance taxonomy; `AssetFinance` and
> `AssetDepreciationEntry` are a **read-only mirror of SAP** and are never
> written by AMS; bulk lines carry a `Quantity` with per-place balances in
> `AssetHolding`. The `FieldAssets` module and schema are **deleted** —
> **15 modules, not 16.** Design: `docs/07ASSETREGISTERDESIGN.md`.
> Everything Revision 3 uncovered on the way in: `docs/00DESIGNDECISIONS.md`.

`AMS_Consolidated_Design_v2.sql` is the **reviewed reference** for the data
model. The EF model is the source of truth for deployment, and migrations must
produce exactly that design. When code and script disagree, fix whichever is
wrong *by decision* — never by drift.

## 3. Rules that are not negotiable

These are the ones most often broken, usually with good intentions.

1. **A module never references another module's implementation.** No project
   reference to `AMS.Modules.X`, no EF navigation across schemas, no `using` of
   another module's entities. Cross-module links hold the **id only**. Talk
   through the other module's **`AMS.Modules.X.PublicApi`** assembly — that one
   you may reference, and it is the only one.
2. **A handler never injects another module's `DbContext`.** If you need to
   write to another module's table, you want its write contract
   (`IAssetTimeline`, `IEmailOutbox`). `UnitOfWorkBehavior` already puts both
   inside one transaction — 01 rule 4a.
3. **Business invariants live in the database, not in a validator.** One
   holder per asset, one pending approval, one active cycle: these are
   filtered unique indexes. Catch SQL error 2601/2627 and return 409. **Never
   pre-check with a read** — a read-then-write check is a race with a nicer
   error message.
4. **Expected failures are `Result<T>`, not exceptions.** Throw only for bugs.
5. **Authorisation is by capability, never by role name.** Server-side, always;
   a client-side check is courtesy only.
6. **No `DateTime.UtcNow`.** Inject `IClock`. An SLA you cannot test is an SLA
   nobody can argue with when it is wrong.
7. **Strings that exist in a database CHECK constraint are smart enums**,
   spelled exactly as the schema spells them. Never retype `"HandedOver"`.
8. **Nothing is hard-deleted.** Assets soft-delete, tickets close, approval
   history is undeletable by FK design. Do not "fix" those FKs.

## 4. Where code goes

```
src/
  Backend/                  the .NET solution
    AMS.Api/                the ONE host; the only project that references every module
    AMS.SharedKernel/       Result<T>, Error, IClock, ICurrentUser. References NOTHING.
    AMS.Infrastructure/     auth, outbox drain, file storage, clock, document writing
    AMS.Reporting/          read-only cross-schema queries. Never writes.
    Modules/AMS.Modules.X/  one per schema
    tests/
      AMS.ArchitectureTests/  the rules above, enforced
  Web/                      Angular client
  Mobile/                   Flutter audit app
```

`tests/` sits INSIDE `src/Backend/`, so the architecture rules exclude it
explicitly when they enumerate production projects — see `Solution.LoadProjects`.
A test referencing three modules at once is normal; a module doing it is the
thing those rules exist to catch.

Inside a module:

```
Domain/           entities, value objects, rules. No EF types.
PublicApi/        interfaces + DTOs other modules may depend on
Persistence/      DbContext + one IEntityTypeConfiguration per entity
Features/<Slice>/ one folder per use case
```

A slice is **exactly seven files**: `Command` *xor* `Query`, then `Request`,
`Response`, `Validator`, `Handler`, `Mapper`, `Endpoint`. Nothing else may sit
in a slice folder. Shared logic moves **down** into `Domain/`, never sideways.

There is no `Common/`, `Shared/`, `Helpers/`, `Utils/` or `Misc/` folder in a
module, and the architecture tests fail the build if one appears.

## 5. Build, test, and what is enforced for you

```bash
dotnet build AMS.slnx              # warnings are errors; style violations are errors
dotnet test  AMS.slnx              # architecture rules + persistence gates
./build/Compare-Schema.ps1         # the EF model still produces the reviewed design
```

Run the third one whenever you touch an entity configuration. It builds one
database from `AMS_Consolidated_Design_v2.sql` and one from the migrations, and
fails on any difference in a column, index, filter, foreign key, delete rule or
CHECK. It needs a local SQL Server; the default is `.\SQLEXPRESS2022`.

Settings are central and deliberate. Do not override them per project:

- `Directory.Build.props` — `net10.0`, nullable, **`TreatWarningsAsErrors`**,
  `EnforceCodeStyleInBuild`, NuGet vulnerability audit.
- `Directory.Packages.props` — **every package version**. A `Version=` in a
  `.csproj` is a build error. Adding a package means adding it here.
- `.editorconfig` — formatting and naming, as **errors**. Where a rule is
  deliberately switched off, the reason is written beside it. Add to that list
  the same way: centrally, with a reason, in its own PR.

So a lot of this document is not on your honour: the build will simply refuse.
What the build cannot check is in section 3, and that is where review looks.

## 6. If you are an AI coding tool, read this twice

- **Do not invent a convention.** If this file and `docs/` do not cover it,
  say so and ask. A plausible invention is worse than a question, because it
  looks decided.
- **Do not relax a rule to make code compile.** Turning off an analyzer,
  adding `#pragma warning disable`, or loosening a constraint to get green is
  a change to the standards and needs to be argued for on its own.
- **Do not add a package** without adding it to `Directory.Packages.props`,
  and do not add one at all where something already referenced will do.
- **Mirror the schema exactly.** Every max length, CHECK, default, filtered
  index and constraint name in an entity configuration must match
  `AMS_Consolidated_Design_v2.sql` character for character, including the
  constraint and index *names*.
- **When you change a standard, change the document too.** A rule enforced in
  `.editorconfig` but absent from `docs/` will be undone by the next person.
- **Every deviation from the design gets an entry in `docs/00DESIGNDECISIONS.md`
  on the day it is made** — what changed, why, what it cost, where the evidence
  is. An undocumented deviation is indistinguishable from a mistake, and the
  next person will "fix" it back.

## 7. Commits

- Explain **why**, not what — the diff already says what.
- No AI-assistant attribution in commit messages, branch names, PR titles or
  code comments.
- One migration per PR, named `{yyyyMMdd}_{Module}_{WhatChanged}`. Never edit
  an applied migration.
