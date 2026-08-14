# AMS — 04 Frontend Standards (Angular + DevExtreme 26.x)

The web application mirrors the backend's shape: **feature folders per module**, the same names as the API modules, and no cross-module imports except through shared contracts.

---

## 1. Project layout

```
web/src/app/
├─ core/                       # singletons: auth, capability guard, http interceptors, clock
│  ├─ auth/
│  ├─ capabilities/            # Capabilities const mirror of backend
│  ├─ http/                    # api-base, error, etag, correlation-id interceptors
│  └─ layout/                  # shell, nav, notification bell
├─ shared/                     # dumb reusable ui + devextreme wrappers, pipes, models
│  ├─ ui/
│  │  ├─ ams-grid/             # the ONE wrapper around dx-data-grid
│  │  ├─ ams-form/             # wrapper around dx-form conventions
│  │  └─ ams-lookup/
│  └─ util/
└─ modules/
   ├─ identity/
   ├─ organization/
   ├─ assets/
   ├─ allocations/
   │  ├─ allocations.routes.ts
   │  ├─ data/                          # api services + request/response models
   │  │  ├─ allocations.api.ts
   │  │  └─ models/
   │  │     ├─ allocate-asset.request.ts
   │  │     ├─ allocate-asset.response.ts
   │  │     └─ handover.models.ts
   │  └─ features/                      # one folder per screen (catalogue names)
   │     ├─ allocation-list/
   │     ├─ my-assets/
   │     ├─ branch-handover/
   │     │  ├─ branch-handover.page.ts        # standalone component
   │     │  ├─ branch-handover.page.html
   │     │  ├─ branch-handover.store.ts       # signal store for the screen
   │     │  └─ condition-photos.component.ts
   │     └─ customer-sites/
   ├─ movements/  ├─ transfers/  ├─ service-desk/  ├─ service-level/
   ├─ contracts/  ├─ verification/  ├─ discovery/   ├─ sap-sync/
   ├─ notifications/ ├─ audit/      ├─ data-import/ ├─ field-assets/
   └─ reports/
```

- **Standalone components + signals**; no NgModules. Change detection `OnPush` everywhere (zoneless when the DevExtreme integration allows).
- Lazy-load every module via `loadChildren` on the route; the shell knows only routes and capability requirements.
- One screen = one `*.page.ts` + optional child components + one `*.store.ts`. No component over ~300 lines; split.
- Models in `data/models/` are hand-written mirrors of the API `Request`/`Response` records (or generated from OpenAPI — if generated, never edited by hand).

## 2. TypeScript rules

- `strict: true`, `noUncheckedIndexedAccess: true`, ESLint + Prettier enforced in CI.
- No `any` (use `unknown` and narrow). No non-null `!` except directly after a guard.
- Dates cross the wire as ISO UTC strings; convert at the edge with a `UtcDate` pipe/util. **Never `new Date(str)` scattered in components.**
- All state in signal stores; components read signals, call store methods. No business logic in templates; no logic in components beyond view concerns.
- Capability checks: `*amsCan="Capabilities.Allocations.HandoverRecord"` structural directive + route guards. **Never check role names in the client** — same law as the backend.

## 3. DevExtreme 26.x — the rules that keep it robust

**One licence, one theme, one wrapper.**

- All grids go through `<ams-grid>` which owns: server-side `CustomStore` wired to the backend's DevExtreme load contract (`skip/take/sort/filter/group/totalCount`), Excel export button (catalogue: "every grid exports"), column chooser, state persistence (localStorage key = route), and the standard error toast.
- **Server-side always** for business tables: `remoteOperations: { paging: true, filtering: true, sorting: true, grouping: true }`. Client-side operations are allowed only for lookup tables under ~200 rows.
- `keyExpr` is always the entity `Id`. `dataSource` is always a `CustomStore` from `data/` services — components never build URLs.
- Editing: **never use inline grid CRUD against business tables.** Grids navigate to a form page (command slice) — the API is command-based, not REST-CRUD, and inline editing hides validation and concurrency.
- Forms: `dx-form` with items generated from a typed config; validation adapters display FluentValidation errors returned in `ValidationProblemDetails` (`errors[field][]`) next to the matching editor; unmapped errors go to a form-level summary.
- Concurrency: forms carry the response `etag`; on 412 show the standard "This record changed while you were editing" dialog with reload/compare options — one shared handler, not per-screen.
- 409 conflicts (filtered-index law: double allocation, duplicate submission) surface the server's message verbatim in a toast — the server writes readable 409s on purpose.
- Lookups (`dx-select-box`/`dx-lookup`) use paged server search (`searchTimeout: 400`, min length 2) for employees/assets; never load the whole employee table.
- Dates in grids display in the **viewer's locale**, tooltips show the UTC instant; branch wall-clock times (operational hours) are edited as `HH:mm` with the branch time zone shown beside the editor — mirroring the DB model exactly.
- Theme: single DevExtreme theme built via ThemeBuilder, checked into `web/themes/`; no per-component style overrides of DX internals (`::ng-deep` on DX classes is a review-blocker).
- Version pinning: DevExtreme pinned exact (`26.1.x`), upgraded alone in a dedicated PR with visual regression screenshots of the five worst grids.

## 4. HTTP layer

- One `ApiService` base: typed `get/post`, correlation-id header, etag capture, camelCase↔PascalCase handled by the API's JSON options (client does not transform).
- Errors: interceptor maps ProblemDetails → typed `ApiError`; 401 → refresh/login flow; 403 → "not permitted" page with the missing capability named; 5xx → global toast + correlation id displayed for support.
- No `HttpClient` outside `data/` services. No calls from constructors — data loads in route resolvers or store `load()`.
- File upload/download (attachments, condition photos, contract documents, import files) goes through one `FilesService` with progress; uploads send content type + size so the server can store the metadata columns the schema added.

## 5. Screens follow the catalogue

The Angular navigation is the catalogue's screen list, gated by capabilities:
Service Desk ticket queue sorts **overdue first** using the server's SLA queue endpoint; "My Approvals" polls/receives the approver's pending steps; the import wizard is rehearse → review rejections grid → commit; Field Assets menu renders only for `field-asset.view`.

## 6. Testing & quality gates

- Unit: stores and pipes (Jest/Vitest). Component tests for the two shared wrappers (`ams-grid`, `ams-form`) are mandatory and deep.
- E2E (Playwright): one happy path per module minimum — allocate→handover→despatch→GRN is the golden flow; ticket raise→assign→resolve is the second.
- Accessibility: DX components carry labels; every form editor has `label`/`aria`; keyboard-only pass on the golden flows.
- Bundle budget per lazy module: 400 kB gz warning, 600 kB error. DevExtreme imported per-component (`devextreme-angular/ui/data-grid`), never the barrel.

## 7. Review checklist

- [ ] New screen sits in the right module folder with page + store split
- [ ] Grid uses `<ams-grid>` + server-side operations; export works
- [ ] No role-name checks; capability constants only
- [ ] 400/409/412 all handled through the shared handlers
- [ ] No `any`, no direct `HttpClient`, no `::ng-deep` on DX internals
- [ ] Dates via the shared pipes; wall-clock times keep their branch time zone
