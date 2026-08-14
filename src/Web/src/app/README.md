# Layout

The folders here are the ones `docs/04FRONTENDANGULARDEVEXTREME.md` §1 defines. They are
empty on purpose — the skeleton is checked in so the first screen has nowhere wrong to go.

```
core/       singletons only: auth, capability guard, http interceptors, layout shell
shared/     dumb reusable UI, the DevExtreme wrappers, pipes, models
modules/    one folder per API module, named exactly as the module is named
```

## What goes inside a module folder

A module is not opened up until it has a screen. When it does, it takes this shape:

```
modules/allocations/
├─ allocations.routes.ts          # lazy-loaded via loadChildren, capability on the route
├─ data/                          # the ONLY place HttpClient appears
│  ├─ allocations.api.ts
│  └─ models/                     # one file per Request/Response, mirroring the API records
└─ features/                      # one folder per screen, named from the catalogue
   └─ branch-handover/
      ├─ branch-handover.page.ts      # standalone, OnPush
      ├─ branch-handover.page.html
      └─ branch-handover.store.ts     # signal store, all state and logic
```

One screen = one page + one store. No component over ~300 lines. No cross-module imports —
if two modules need the same thing it belongs in `shared/`, never sideways.

## The shell

`core/layout/` holds the application chrome, built from DevExtreme components:

| Piece | Component | Notes |
|---|---|---|
| Navigation | `dx-drawer` + `dx-tree-view` | Shrink mode; collapses to a 60px icon rail |
| Application bar | `dx-toolbar` | Drawer toggle, search, bell, user menu |
| Global search | `dx-text-box` | `Ctrl`/`Cmd` + `K` focuses it |
| User menu | `dx-drop-down-button` | |
| Notification panel | `dx-popup` | Right-docked, full height |
| Toaster | `notify()` via `ToastService` | One service, four kinds |

Two things here were found the hard way and should not be "simplified" back:

- The notification panel is a **popup, not a second drawer**. Nesting two `dx-drawer`s put
  the right panel one viewport-width off-screen while it still reported itself open.
- The panel component sets its own **width**. In `overlap` mode a drawer sizes to its panel
  content, so a host with no width collapses to nothing and looks like a dead button.

## Not built yet

`shared/ui/ams-grid`, `ams-form` and `ams-lookup` are the three wrappers everything else
goes through, and they bind to contracts the API does not expose yet (the DevExtreme load
contract, `ValidationProblemDetails` shapes, etag/412 handling). They land with the first
command slice, not before — see §3 and §4 of the standards for what they must own.

## DevExtreme

`devextreme` and `devextreme-angular` are pinned **exact** at 26.1.3 — the same 26.1.x as
`DevExpress.Document.Processor` on the server. Import per component
(`devextreme-angular/ui/data-grid`), never the barrel.

The licence key is **not** in source control. `devextreme-license` generates
`src/devextreme-license.ts` from the DevExpress licence registered on the machine, and
`prestart`/`prebuild` run it, so a fresh clone needs the machine licence but no manual step.
`main.ts` applies the key before bootstrap — a component created first shows the evaluation
banner.

## Theme

`themes/` holds the Fujitec bundles; `npm run theme:build` compiles them to
`public/assets/themes/` (gitignored) and `index.html` activates them with `rel="dx-theme"`.
`data-theme` deliberately keeps the stock `material.orange.*` names because DevExtreme looks
chart styling up by theme name. Both bundles copy the stock `@use` list verbatim — dropping
an entry silently drops those components' styles.

## Bundle budgets

`docs/04 §6` specifies 400 kB **gzipped** warning / 600 kB error, but Angular budgets can
only measure **raw** bytes. The numbers in `angular.json` are raw proxies, set from the
ratio this app actually compresses at (1.42 MB raw → 310 kB transfer, ≈4.6×): 1.8 MB warning,
2.7 MB error. Re-derive them if that ratio moves.
