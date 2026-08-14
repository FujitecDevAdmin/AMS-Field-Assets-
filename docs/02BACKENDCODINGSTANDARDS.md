# AMS — 02 Backend Coding Standards (.NET 10, C#, Minimal API, CQRS)

Applies to everything under `src/`. Boundaries and slice layout are defined in `01ARCHITECTURE.md`; this document is how the code inside them is written.

---

## 1. Language and project settings

- `net10.0`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
- One `.editorconfig` at the repo root is law; the build fails on style analyzers (IDExxxx as errors for naming, unused usings, etc.).
- File-scoped namespaces. One public type per file; the file is named after the type.
- `record` for commands, queries, requests, responses, DTOs, domain events. `class` for entities and handlers. `readonly record struct` for small value objects (ids, money).
- No `dynamic`. No `#region`. No partial classes except source-generator targets.

## 2. Naming

| Thing | Pattern | Example |
|---|---|---|
| Command | `{Verb}{Entity}Command` | `AllocateAssetCommand` |
| Query | `Get{X}Query` / `Search{X}Query` | `GetMyAssetsQuery` |
| Request/Response | `{Slice}Request` / `{Slice}Response` | `AllocateAssetRequest` |
| Validator | `{Slice}Validator` | `AllocateAssetValidator` |
| Handler | `{Slice}Handler` | `AllocateAssetHandler` |
| Endpoint | `{Slice}Endpoint` | `AllocateAssetEndpoint` |
| Interface | `I{Noun}` in `PublicApi/` | `IAssetLookup` |
| Async methods | suffix `Async`, always take `CancellationToken ct` | `HandleAsync(cmd, ct)` |
| Booleans | `Is/Has/Can` prefix (matches DB rule 8) | `IsReceivedByHo` |
| Constants for capability names | `Capabilities.{Module}.{Name}` | `Capabilities.Allocations.RevertReturn` → `allocation.revert-return` |

The constant is grouped by the **owning module** — the `Module` column of `Identity.Capability` — while the string keeps the prefix the schema seeded it with. The two do not always agree: `handover.dispatch` and `handover.receive` are owned by Movements, so they live at `Capabilities.Movements.HandoverDispatch`. Follow the seed; never rename a capability string to make the constant tidier, because roles are already mapped to the string.

Strings that exist in the database CHECK constraints (statuses, kinds, conditions) are **smart enums / value objects**, defined once per module, with the literal spelled exactly as the schema spells it. Never retype `"HandedOver"` at a call site.

## 3. The Result pattern

```csharp
public sealed record Error(string Code, string Message)
{
    public static Error NotFound(string what, object id) => new($"{what}.NotFound", $"{what} {id} was not found.");
    public static Error Conflict(string code, string msg)  => new(code, msg);
    public static Error Validation(string code, string msg) => new(code, msg);
}

public sealed class Result<T> { /* IsSuccess, Value, Error; Bind/Map helpers */ }
```

- Handlers return `Result<TResponse>`. **Never throw for an expected outcome** (not found, already allocated, SLA policy missing). Throw only for genuine bugs and let the global exception handler produce a ProblemDetails 500.
- Error codes are stable, dot-separated, and documented in the slice: `Allocation.AlreadyActive`, `Handover.AssetNotInBranchStore`.
- Endpoint mapping is mechanical: `NotFound→404`, `Validation→400`, `Conflict→409`, `Concurrency→412`.
- Success is **201 with a `Location` header when the command created a row**, 200 with the `Response` in every other case. The slice decides once, in its `*Endpoint.cs` (`Results.Created(...)` vs `Results.Ok(...)`); `ToHttpResult()` cannot infer it and must not guess.

## 4. Handlers

```csharp
public sealed class HandoverToBranchStoreHandler(
    AllocationsDbContext db,
    IAssetTimeline timeline,                                  // Assets PublicApi — NOT its DbContext
    IClock clock,
    ICurrentUser user) : IRequestHandler<HandoverToBranchStoreCommand, Result<HandoverToBranchStoreResponse>>
{
    public async Task<Result<HandoverToBranchStoreResponse>> HandleAsync(
        HandoverToBranchStoreCommand cmd, CancellationToken ct)
    {
        var allocation = await db.Allocations
            .FirstOrDefaultAsync(a => a.Id == cmd.AllocationId && a.ReturnedOnUtc == null, ct);
        if (allocation is null)
            return Error.NotFound("Allocation", cmd.AllocationId);

        var handover = allocation.HandToBranchStore(          // domain method holds the rules
            cmd.Condition, cmd.Remarks, cmd.BranchLocationId, user.Id, clock.UtcNow);

        db.Handovers.Add(handover);
        await db.SaveChangesAsync(ct);                        // 2601 → Conflict via behavior

        // [Assets] is another module's schema, so the timeline goes through its
        // write contract. Same transaction, opened by UnitOfWorkBehavior — 01 rule 4a.
        await timeline.AppendAsync(handover.ToTimelineEvent(), ct);

        return HandoverToBranchStoreMapper.ToResponse(handover);
    }
}
```

Rules:

- Constructor injection via primary constructors; **no service locator, no static state**.
- One `SaveChangesAsync` per command handler **per module context**. The *transaction* is the unit of work and `UnitOfWorkBehavior` owns it (01 rule 4a), so a handler that also appends a timeline or queues mail calls `SaveChanges` on its own context and lets the write contract save on its own — both inside the one transaction. Multi-aggregate writes within a module stay in a single `SaveChanges`.
- A handler **never** injects another module's `DbContext`. If you need one, you want that module's `PublicApi` write contract instead; the architecture test fails the build otherwise.
- Query handlers use `AsNoTracking()`, project straight to the `Response` with `Select`, and never call `SaveChanges`.
- No `AutoMapper`. Mapping is explicit in `{Slice}Mapper.cs` static methods — boring, greppable, compile-checked.
- `DateTime.UtcNow` is forbidden; inject `IClock`. `Guid.NewGuid()` in domain logic is forbidden; ids come from the DB or the client (idempotency GUIDs).

## 5. Validation (FluentValidation)

- Validators check **shape**: required, lengths (mirror the schema lengths — `Remarks` 500, `Subject` 300), enum membership, date sanity.
- Validators never touch the database. "Asset exists", "one holder", "one active cycle" are handler + database concerns (filtered unique indexes are the law; catch, don't pre-check — a read-then-write check is rule 6's anti-pattern).
- Every `Request` has a validator, even if trivial — the pipeline requires one so nobody forgets when fields are added.
- Register by scanning each module assembly once at startup.

## 6. Minimal API endpoints

```csharp
public static class HandoverToBranchStoreEndpoint
{
    public static void Map(RouteGroupBuilder g) => g
        .MapPost("/handovers", async (HandoverToBranchStoreRequest req, IDispatcher d, CancellationToken ct) =>
            (await d.SendAsync(HandoverToBranchStoreMapper.ToCommand(req), ct)).ToHttpResult())
        .RequireCapability(Capabilities.Allocations.HandoverRecord)
        .WithName("HandoverToBranchStore")
        .Produces<HandoverToBranchStoreResponse>(201)
        .ProducesValidationProblem()
        .Produces(409);
}
```

- Endpoints contain **zero** logic: bind → map → dispatch → translate. If you feel an `if` coming on, it belongs in the handler.
- Auth: `RequireCapability(...)` extension resolves the caller's effective capabilities (role union + per-user override, deny wins) from the Identity module contract. Never check role names.
- Branch scoping is applied inside query handlers via the caller's branch set (`ICurrentUser.BranchIds` / `HasAllBranches`) — per request, not a global EF filter (schema appendix rule).
- OpenAPI on for all endpoints; every endpoint has `WithName` (stable operation ids feed the Angular client generator).

## 7. Logging, errors, observability

- `ILogger<T>` with **message templates**, never interpolation: `_log.LogInformation("Handover {HandoverId} recorded for asset {AssetId}", …)`.
- No secrets, passwords, license keys, or MFA data in logs — the same exclusion list as the audit interceptor.
- Correlation id middleware; the id flows into every log line and into `ProblemDetails.extensions.traceId`.
- One global exception handler → RFC 7807 ProblemDetails. Handlers do not try/catch except to translate a specific, known failure.

## 8. Async and performance

- Async all the way; no `.Result`, `.Wait()`, `async void`. `CancellationToken` accepted and passed on every async call.
- Lists are paged server-side always (DevExtreme load contract); an unbounded `ToListAsync` on a business table is a review-blocker.
- `IQueryable` never leaks out of a handler.

## 9. Dependency injection

- Each module ships `AddAllocationsModule(this IServiceCollection, IConfiguration)` that registers its DbContext, handlers, validators, and PublicApi implementations. `Program.cs` is a list of `builder.Services.Add*Module(...)` calls and nothing else.
- Lifetimes: handlers/validators scoped, lookups scoped, `IClock`/config singleton. No captive dependencies.

## 10. Review checklist (PR gate)

- [ ] Slice has all its files; nothing added to a "Common" dumping ground
- [ ] Handler returns `Result<T>`; no expected-path exceptions
- [ ] Business invariant enforced in domain/DB, not in the validator
- [ ] Timeline/outbox rows written in the same SaveChanges as the change
- [ ] No cross-module project reference introduced (architecture test green)
- [ ] Strings matching DB CHECK values come from the smart enum
- [ ] New capability added to the schema seed AND `Capabilities` constants — Section 17.6 for the base design, the approval extension's own Section 5 for anything under `new-service.*`
- [ ] Integration test covers the DB-enforced rule the slice relies on
