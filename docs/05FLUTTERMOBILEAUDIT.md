# AMS — 05 Flutter Mobile Audit (offline-first physical verification)

The audit app does one job: somebody stands in front of an asset and confirms it is there. It is the only part of AMS that runs where there is no network, so everything below follows from one assumption — **the device is the system of record until the moment it syncs, and it may hold that role for days.**

Boundaries are defined in `01ARCHITECTURE.md`. The app talks to `AMS.Modules.Verification` and to nothing else. Its data model is Section 10 of `AMS_Consolidated_Design_v2.sql`.

---

## 1. Project layout

```
mobile/
├─ lib/
│  ├─ main.dart
│  ├─ core/
│  │  ├─ auth/                     # token store, refresh, offline session
│  │  ├─ api/                      # dio client, interceptors, error mapping
│  │  ├─ db/                       # drift database, DAOs, migrations
│  │  └─ sync/                     # the outbox drainer (see 4)
│  ├─ features/
│  │  ├─ cycle/                    # download and cache the active cycle
│  │  ├─ scan_verify/              # the capture screen
│  │  │  ├─ scan_verify_page.dart
│  │  │  ├─ scan_verify_controller.dart
│  │  │  └─ widgets/
│  │  └─ outbox/                   # the queue screen
│  └─ shared/                      # widgets, formatters, result types
└─ test/
```

Two screens ship: **Scan & Verify** and **Offline Queue**. The catalogue names them; do not add a third without adding it there first.

## 2. Language and state rules

- Latest stable Flutter and Dart, `analysis_options.yaml` with `strict-casts`, `strict-raw-types`, and lints as **errors** in CI.
- State: Riverpod. One controller per screen, `AsyncValue` for anything that can fail. No `setState` outside a leaf widget's own animation.
- No business rule in a widget. A widget reads state and calls the controller.
- `Result<T>` mirrors the backend: expected failures are values, exceptions are bugs. The same discipline as 02 §3.
- Time: every instant stored or sent is **UTC ISO-8601**. Display converts at the edge; nothing else touches a local `DateTime`.

## 3. The local database is not a cache

`drift` over SQLite, migrated with numbered schema versions exactly like EF migrations. Three kinds of table, and the difference matters:

| Kind | Example | Rule |
|---|---|---|
| **Reference** | the active cycle, the assets in scope, status lookups | Downloaded, read-only, replaceable. Never edited on the device. |
| **Capture** | a `PhysicalVerification` the technician just recorded | Written once on the device, immutable afterwards. |
| **Outbox** | one row per capture awaiting upload | The only mutable table. Holds attempt count, last error, state. |

A capture is written to the capture table and its outbox row **in one SQLite transaction**. This is the same rule as the server's rule 4a, for the same reason: a queue that can disagree with the thing it is queueing is worse than no queue.

Captures are never deleted on success. They are marked synced and kept until the cycle closes, so a technician can prove what they recorded.

## 4. Sync

One drainer, running on connectivity regain, on app resume, and on a manual pull-to-refresh. Never on a timer that fires while the screen is dark.

- **Serial, oldest first.** No parallel uploads. A cycle of a thousand assets is not a throughput problem; a duplicate is.
- Exponential backoff per row (2s → 5m cap), attempt count persisted. After 10 failed attempts the row moves to `NeedsAttention` and shows on the Offline Queue screen with the server's message — it does not retry forever in silence.
- One capture = one `POST /api/v1/verification/cycles/{cycleId}/verifications`, `multipart/form-data`: the JSON body plus the photo. The photo travels **with** the capture, not before it. A photo that uploaded and a capture that did not is an orphan file nobody will ever look for.
- The request carries `Content-Type` and byte size so the server can fill the upload-metadata columns the schema added.
- Photos are downscaled on device to a long edge of 1600 px, JPEG quality 80, before they enter the outbox. Evidence, not photography.

### Idempotency

The device may send the same capture twice — a response lost on a dying connection is indistinguishable from a request never received.

The device generates a **`ClientCaptureId`** (UUID v4) when the technician records the capture, stores it with the row, and sends the *same* value on every retry for the life of that capture. Never regenerate it; a new id on retry is a new capture, and the whole mechanism is gone. This is the `ClientDecisionId` pattern `01ARCHITECTURE.md` §5 names as the house idempotency convention.

Two database indexes then separate the two cases that look identical from the client:

| Server sees | Means | App shows |
|---|---|---|
| 2601 on `UX_PhysicalVerification_ClientCapture` | this phone already sent this capture | nothing — mark synced, it worked |
| 2601 on `UX_PhysicalVerification_OnePerAssetPerCycle` | another technician verified this asset first | "Already verified by {name} on {date}" — keep the capture, mark it superseded |

The server answers the first case with the existing row and 200, not an error. Only the second is a real conflict.

## 5. What a capture records

Mapped straight onto `Verification.PhysicalVerification`:

- **QR scan** → `ScannedQrValue`. If the scanned tag does not resolve to the expected asset, set `HasQrMismatch` and **let the capture proceed**. A mismatch is the single most valuable row in the audit; an app that refuses to record it converts a finding into nothing.
- **Serial** → `SerialVerified`, confirmed against the cached register value. Shown, not typed, wherever the register has one.
- **Working condition** → `WorkingCondition`, one of `Good` · `MinorDamage` · `Damaged` · `NotWorking` · `Missing` (`CK_PhysicalVerification_Condition`). A smart enum spelled exactly as the schema spells it, defined once (02 §2). The app **mirrors** this list; it never defines or extends it. It is deliberately the same vocabulary as a return condition, so the same word means the same thing wherever an asset is judged.
- **GPS** → `GpsLatitude` / `GpsLongitude`, `decimal(9,6)`; round to 6 dp on the device, do not send more. Capture is best-effort: indoors it may be absent, and both columns are nullable on purpose. Never block a capture on a fix.
- **Photo** → `PhotoPath`, assigned by the server on receipt. The device holds a local path and never invents the server's.
- **Time** → `VerifiedOnUtc` is the **device** clock at capture, because offline there is nothing else. `CreatedOnUtc` is the server's own. Both are kept; when they disagree by more than an hour the sync response says so and the Offline Queue shows it. Do not silently trust either.

## 6. Cycles, scope and the closed-cycle problem

- Exactly one cycle is active (`UX_PhysicalVerificationCycle_OneActive`). The app caches that cycle and the asset list for the technician's branch scope, resolved server-side per request (never a client-side filter).
- **A cycle can close while a technician is offline.** The server must reject captures against a closed cycle, and the app must show those rows as `Rejected — cycle closed on <date>` with the capture still readable. Losing a day of fieldwork to a silent discard is how people stop using the app. This is the edge case to write the integration test for first.
- No active cycle cached → the capture screen is unavailable and says why. It does not queue captures against a cycle it cannot name.

## 7. Errors, auth and security

| Response | Meaning | App behaviour |
|---|---|---|
| 201 | recorded | mark synced |
| 200 | our own `ClientCaptureId` already landed | mark synced, say nothing |
| 409 | another technician verified this asset first | keep the capture, mark superseded, name who and when |
| 422 / 400 | the payload is wrong | `NeedsAttention`, never retried unchanged — retrying a rejected body is a loop |
| 401 | token expired | refresh once, then re-queue; if refresh fails, hold and prompt for sign-in |
| 5xx / timeout | unknown | retry with backoff |

- Capability `verification.capture` gates the endpoint. The app checks the cached capability set to hide UI, and the server checks it again — the client check is courtesy, the server check is the rule.
- Tokens in platform secure storage (Keychain / Keystore). Never in SQLite, never in `SharedPreferences`.
- Offline sessions are bounded: a refresh token unusable for 14 days locks the app and requires sign-in. A lost phone is not an indefinite credential.
- The local database is encrypted (SQLCipher) and photos live in app-private storage, not the camera roll.
- Certificate pinning on the API host. Screenshots disabled on the capture screen is *not* required — evidence is not secret.

## 8. Testing and quality gates

- **Unit**: controllers and the sync drainer, with a fake API. The drainer's backoff, attempt-count and terminal-state transitions are all covered.
- **Integration** (the ones that matter, all against a real API + SQL Server):
  - capture offline → go online → syncs exactly once;
  - the same capture uploaded twice → one row, second answers 200 with it, both marked synced;
  - two technicians verify one asset → first wins, second gets 409 naming the first;
  - cycle closes mid-flight → rejected, capture preserved, message shown;
  - QR mismatch → recorded with `HasQrMismatch`, not blocked.
- **Widget** tests for the capture screen's required-field and mismatch paths.
- No release without a manual airplane-mode pass: capture five assets offline, force-quit, reopen, reconnect. Nothing lost, nothing doubled.

## 9. Review checklist

### Responsive typography

All routes inherit `ResponsiveTypography` from the app builder. Use logical
screen width, never physical screen resolution, for modest typography changes.
Do not shrink the system font scale or replace its nonlinear accessibility curve
with a linear estimate. Normal text retains its baseline size on narrow phones;
wider devices add up to 12 percent. Supporting labels use at least 12 logical
pixels and body text uses 14–16. Input fields must use minimum rather than fixed
heights; paired location/filter controls and navigation grow with scaled text.
Test narrow/wide phones and 1.0, 1.5, and 2.0 system text scales.

- [ ] Capture and outbox row written in one local transaction
- [ ] Nothing retried in parallel; backoff and attempt count persisted
- [ ] `ClientCaptureId` generated once at capture and never regenerated on retry
- [ ] Our own resend and somebody else's prior verification are told apart, and worded differently
- [ ] Condition and status strings come from the smart enum, spelled as the schema spells them
- [ ] `VerifiedOnUtc` is device time, sent as UTC, never rewritten by the server silently
- [ ] Photo downscaled, sent with the capture, path assigned by the server
- [ ] Nothing blocks on a GPS fix
- [ ] Closed-cycle rejection is visible and the capture survives it
- [ ] No token or capture data outside secure storage / encrypted SQLite
