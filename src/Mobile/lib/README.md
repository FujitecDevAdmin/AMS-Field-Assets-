# Layout

The folders here are the ones `docs/05FLUTTERMOBILEAUDIT.md` §1 defines, empty until the
code that belongs in them is written.

```
core/auth/    token store, refresh, offline session — tokens in Keychain/Keystore, never SQLite
core/api/     dio client, interceptors, error mapping
core/db/      drift database, DAOs, numbered migrations
core/sync/    the outbox drainer
features/     cycle/ (download and cache the active cycle), scan_verify/, outbox/
shared/       widgets, formatters, Result<T>
```

Two screens ship: **Scan & Verify** and **Offline Queue**. The catalogue names them; do
not add a third without adding it there first.

## Not built yet

`core/db/` and `core/sync/` are the heart of this app and they are deliberately empty. The
drift schema is Section 10 of `AMS_Consolidated_Design_v2.sql` and the drainer's behaviour
is defined by server responses — 200 vs 409 on a duplicate `ClientCaptureId`, rejection on
a closed cycle — that no endpoint returns yet. Writing them against a guess means writing
them twice, and the parts that must not be guessed are exactly the parts that matter:

- capture row and outbox row in **one** SQLite transaction;
- `ClientCaptureId` generated once at capture, never regenerated on retry;
- serial upload, oldest first, backoff persisted, `NeedsAttention` after 10 attempts.

See §3–§5 before starting any of it, and §8 for the integration tests that come with it.

## Dependencies

Only `flutter_riverpod` is in `pubspec.yaml`. `dio`, `drift`, `sqlcipher_flutter_libs`,
`flutter_secure_storage`, the camera/QR packages and the connectivity listener are added
with the layer that uses them, not in advance.

## Launcher icons

Not configured. `assets/images/fujitec-logo.png` is the wordmark (792×214) — it is the
wrong shape for an app icon. A square mark at 1024×1024 is needed before wiring
`flutter_launcher_icons`; the 32×32 web favicon is too small to upscale.
