# AMS — 00 Design Decisions and Deviations

Every departure from the reviewed design, and every decision the standards did
not already settle, is recorded here **on the day it is made**.

The reason this file exists: `AMS_Consolidated_Design_v2.sql` is the reviewed
reference and the five standards documents describe how it is built. When
reality disagrees with either — because SQL Server behaves differently than
assumed, or an analyzer rejects a documented shape — the fix is a **decision**,
written down, never a quiet edit. Several developers on several different tools
work here; an undocumented deviation is indistinguishable from a mistake, and
the next person will "fix" it back.

**Format.** Newest first. Each entry says what changed, why, what it cost, and
where the evidence lives. If there is no evidence, say so.

---

## 2026-08-17 — Rename the Organization location master to Branch

The organization master formerly named `[Organization].[Location]` is now
`[Organization].[Branch]`. Its identifying fields are `BranchId`, `BranchCode`
and `BranchName`, including employee and identity user-branch assignments.
This removes the ambiguity between an administrative branch and a physical
asset/audit location. Location terminology remains valid where a record
describes a physical place rather than the Organization branch master.

Cost: API routes and JSON contracts under Organization change from locations
to branches, and deployment requires data-preserving table/column renames.
Evidence: Organization and Identity EF models and their 20260817 rename
migrations.

---

## 2026-08-13 — Retain the imported FAR row for register drill-down

`Assets.Asset` gains nullable `ImportedDataJson nvarchar(max)`. The Field Asset
Admin register deliberately shows ten operational columns, but the source FAR
contains seventy fields spanning finance, purchase, insurance, assignment and
audit evidence. Several values do not have an editable AMS-owned home, yet an
administrator must be able to inspect exactly what was uploaded.

The importer retains the original row as JSON and refreshes it when an existing
asset number is uploaded again. Structured columns and related tables remain the
queryable source of truth; JSON is display/audit evidence only. Cost: one
potentially wide nullable value on imported rows. Evidence: the 70-column
template, import validator, and Field Assets View popup.

## 2026-08-12 — Discovery, an endpoint with no user, and a duplicate device

Nine slices, thirty-six tests. Six tables fed by software rather than by
people.

### The agent is not a user

Its endpoint has **no capability**, and R3-13 says so in the seed's comment. An
agent has no session, no branches and nobody to grant anything to; it presents
an API key and holding a live one is the whole of the authorisation. Inventing
a capability for it would mean a user account per machine, which is how service
accounts multiply.

That makes it the second anonymous route in the system after sign-in, and it is
mapped outside the authorised group deliberately — requiring a bearer token
would make the module unreachable by the software it exists to serve.

### The key is hashed with SHA-256, not a password KDF

An API key is 256 bits of randomness this system generated. There is no
dictionary to attack and no user who reused it on another site, so the slow
hashing that protects a chosen password buys nothing — and it would be paid on
every inventory post from every machine in the company.

`KeyPrefix` is stored in the clear and is what makes authentication one indexed
read rather than a hash of every key in the table. Comparison is fixed-time
anyway.

**Every rejection says the same thing.** No key, unknown key, revoked key: one
message. Telling an agent which would tell anybody probing the endpoint which
of their guesses was closest. A test holds it.

**Forbidden, not 401.** The kernel has no `Unauthorized` kind and `SignIn`
already answers a bad password with `Forbidden`. Adding a kind for this one
endpoint would leave the codebase answering the same question two ways; the
inconsistency with HTTP convention is noted and deliberate.

### A test found a duplicate device

`FindDeviceAsync` matched on serial, then fell back to hostname **only where
the stored serial was null**. So an agent that read a serial last week and
could not read it today — a driver update, a permissions change, a virtual
machine — created a second device row and silently split the machine's history
in two.

Now: with a serial, match the serial, else a same-hostname device that has none
yet (and fill it in). With no serial, match the hostname alone, most recently
seen. The test that caught it was not aimed at this; it simply reported the
same machine twice with different fields, which is what a real agent does when
something changes.

### Rules the ingest keeps

- **It decides nothing.** A new machine lands in the queue as `New` and waits
  for a person. It may be a contractor's laptop, a test rig, or something
  already on the register under another name — an agent that created assets
  would fill the register with them.
- **Silence is not a mass uninstall.** An agent that could not enumerate
  software sends nothing, and nothing must not be read as "everything was
  removed".
- **Uninstalled software is marked, not deleted.** A licence audit asks what
  was installed during a period, not what is installed today.
- **A title is counted by DISTINCT machine.** Two versions on one laptop is one
  seat; counting rows would make every upgrade look like a breach.
- **Health is one current row plus history.** A screen asking "how is this
  machine" wants one answer; a drive that has been filling for a month is a
  different problem from one that filled yesterday, and only the trend tells
  them apart.

### Undecided is not unlicensed

A title nobody has catalogued comes back as `IsInCatalogue = false`, separate
from `IsOverLicensed`. Showing the two the same way would make every newly
installed title look like a compliance breach — which is how a compliance
screen stops being read.

**Verified:** 817 tests, 0 failures. Parity 1,569 objects across twelve schemas.

**Documents updated:** `AMS_Consolidated_Design_v2.sql` §capability seed (R3-13).

---

## 2026-08-12 — Verification, and telling a retry from a conflict

Five slices, thirty-three tests. Two tables, and the most interesting pair of
indexes in the schema.

### R3-12 · the eighth and last

`verification.run`, `verification.view`, `verification.manage`. Run is separate
from manage because they are different people in different places: a technician
walks a branch with a phone, an administrator opens and closes the cycle from a
desk. Giving the technician the power to close a cycle mid-count is how a count
ends early.

Eight modules in a row (R3-4 through R3-12), and the last whose screens exist.

### The two duplicate cases deserve different words

This is what R2-21 was written for, and it is now used. The phone generates
`ClientCaptureId` at capture and sends the same value on every retry, so:

| Index hit | What happened | What the technician is told |
|---|---|---|
| `UX_PhysicalVerification_ClientCapture` | the same phone sent it twice | here is the row you already made |
| `UX_PhysicalVerification_OnePerUnitAssetPerCycle` | somebody else got there first | a real conflict |

Both are SQL 2601. Without the capture index the server sees only the second
case and has to call every retry a conflict — which, as the design script says,
teaches technicians to ignore conflicts.

The handler checks for the capture id **before** inserting as well as catching
the collision after. The phone may have been out of signal for a day, and being
told "you already sent this" is a better answer than a round trip that ends in
a constraint violation. The catch is still there for two phones racing.

### Counting is not sighting

R3 split the uniqueness rule and this is where it lands: a unit asset is
sighted once per cycle; a bulk line is counted once per **place** per cycle.
Counting the same line at four branches is the correct answer, and the old
single index called it a duplicate.

Three things follow, none of which the schema can see on its own:

- **A bulk count must say where it was counted.** With no location the index is
  on (cycle, asset, null) and four branches look like one place counted four
  times.
- **A single asset cannot be counted**, and a quantity on a sighting is
  refused. The two kinds of row mean different things and mixing them produces
  a variance report nobody can read.
- **Variance is counted minus expected**, and `ExpectedQuantitySnapshot` comes
  from the device — it is what the sheet said when it was issued, which is the
  number the count is actually disputing.

### The phone is trusted about what it saw

`VerifiedOnUtc` comes from the device, because the capture happened when the
technician was standing in front of the asset, not when the signal came back.
Location and holder likewise: what the phone reports beats what the register
says, because if the register thinks the asset is elsewhere, the register is
what is wrong — that is the entire point of doing a physical verification.

A mismatched tag is **recorded, not refused**, for the same reason. The
technician is standing in front of the thing and the tag being wrong is the
finding. Comparison ignores case and surrounding whitespace, because a QR
reader returns what is printed and printers add neither.

### The report puts the worst first

Missing above NotWorking above Damaged above MinorDamage, then mismatched tags.
A report that buries the missing assets under three hundred healthy ones is a
report nobody finishes reading.

**Verified:** 781 tests, 0 failures. Parity 1,493 objects across eleven schemas.

**Documents updated:** `AMS_Consolidated_Design_v2.sql` §capability seed (R3-12).

---

## 2026-08-12 — Contracts, and R3-11

Eight slices, a worker, forty-one tests. **Contracts is complete** — AMCs,
warranties, leases, licences, service agreements and insurance, because R3
widened this past IT and a lease on a building has an expiry date somebody must
be reminded about exactly like an AMC on a laptop.

### R3-11 · the seventh module with no capabilities

`contract.view`, `contract.manage` and `contract-reminder.manage` are seeded.
View is separate from manage because an AMC's expiry is something a branch
administrator needs to SEE — it decides whether a repair is chargeable — while
editing the contract belongs with whoever negotiates it.

That is seven in a row (R3-4 through R3-11), and it is the last module whose
screens exist. The pattern is settled and needs no further restating: **the
seed is written when the screens are, not when the tables are.**

### Reminder windows are rows, and an override replaces rather than adds

They were 60/30/15/7 compiled into a job, and one AMC needing ninety days'
notice meant a release. A null `ContractId` is the organisation default; a
non-null one overrides it.

The override **replaces**. Merging would mean a contract that wants only a
ninety-day warning still gets the seven-day one, with no way to ask for less.
`ReminderWindows` holds that rule on its own, because two things need it — the
detail screen that shows what will happen, and the worker that makes it happen
— and two copies would be two chances for the screen to be a lie.

### The worker is idempotent because of the index, not because it remembers

The design script says so in as many words, and it is now true. Nothing tracks
what yesterday's pass did; it asks `ContractReminderLog`.

Two consequences worth stating:

- **A window opens and stays open.** The worker fires when `daysLeft <=
  DaysBeforeExpiry`, not on equality, so a pass that did not run on the exact
  day still sends today rather than losing the reminder for ever.
- **R2-2 makes renewal work.** The log's unique key includes the expiry the
  reminder was measured against, so a renewed contract — same row, new end date
  — earns its whole ladder again rather than being permanently silent because
  it was reminded about last year's date.

### Smaller decisions

- **The contract number is not editable.** It is how the contract is quoted on
  an invoice, in an e-mail, on a purchase order — references outside this
  system that we cannot see, let alone update.
- **Renewal extends the same row.** Not a new contract: the number is the same,
  the vendor is the same, and the assets do not want re-linking every year.
  Remarks are appended, because last year's reason is still worth having.
- **Retiring is a flag.** A contract that covered an asset last year is what
  explains why a repair was free.
- **The vendor is the default recipient**, because the person who can do
  something about an expiring AMC usually works for them.
- **A reminder that reaches nobody still writes a row.** It says the window
  fired and found no one, which is a configuration problem somebody has to see
  — and it stops the worker rediscovering the same silent window every day.
- **`LicenceKeyProtector` has its own purpose string**, like the SMTP
  password's. A licence key is what a vendor audit asks for and what somebody
  could take with them; it is never projected into a screen and never logged.
- **`IVendorDirectory`** is the third contract on Organization. One reader of
  `Vendor`, not several — purchase orders and warranty claims will want the
  same answer.

**Verified:** 748 tests, 0 failures. Parity 1,446 objects across ten schemas.

**Documents updated:** `AMS_Consolidated_Design_v2.sql` §capability seed (R3-11).

---

## 2026-08-12 — The SLA escalation monitor, and a shared clock nobody reset

Twenty-three tests. **Both workers now exist**; nothing in the schema is
written-but-unused any more.

### Two modules, one job

The rule about when a target is missed is ServiceLevel's; the tickets are
ServiceDesk's. Neither can do the job alone, and either reading the other's
tables is the coupling schema-per-module exists to remove. So
`AMS.Modules.ServiceDesk.PublicApi` is the fifth contract assembly, and the
monitor lives in ServiceLevel, next to the ladder and the calendar.

`ISlaWatchList` is a **generous read and a single narrow write**. Letting the
monitor ask for "tickets past their due date" would put the rule about what
late MEANS in two modules; letting it write anything but a timeline entry would
let a notification job change a ticket.

ServiceDesk and ServiceLevel now each reference the other's PublicApi. That is
a cycle between the modules and not between the assemblies, which is precisely
what the contract assemblies are for — and `ModuleBoundaryTests` still passes,
because it checks implementation references, not contract ones.

### The grace period is measured in operational minutes

`ThresholdPercent` is additive — 150 means half the target again past the due
time — and that extra is measured in the branch's working hours, like the
target it is a percentage of. Measuring the target in working hours and the
grace period in wall clock would make a Friday-afternoon breach escalate over
the weekend, which is the failure the whole calendar exists to prevent.

`SlaCalendar` was extracted for this: the three `Respect*` flags now have two
callers — the calculator that sets due dates and the monitor that decides when
a missed one escalates — and two copies of "ignoring weekends also means
ignoring the Saturday rules" would be two chances for a due date and its
escalation to disagree.

### Rules the monitor keeps

- **A paused ticket is not late.** It is waiting on somebody who is not us, and
  escalating it would be telling a technician off for a delay the requester is
  causing.
- **A response escalation is dropped once somebody has replied** — a complaint
  about a thing that did not happen.
- **A rung that reaches nobody gets a `Skipped` row**, not silence. The rung is
  configured and did not fire, which somebody has to see; the row also stops
  the monitor rediscovering the same empty rung every minute.
- **Failed rows can be retried.** R2-3 excludes them from
  `UX_SlaEscalationLog_OncePerLevel` deliberately, and a test drives that path.
- **The index is a backstop, not the mechanism.** The monitor asks before it
  fires, because a 409 raised inside a background pass is a 409 nobody sees.

### BranchAdmin has no capability column, so the module picks one

`SlaEscalation.RecipientType` allows `BranchAdmin` but carries no capability to
say which one makes somebody a branch administrator — unlike the approval
workflow's `LocationBranchAdmin` rule, which has its own. The monitor uses
`request.manage`, narrowed to the ticket's branch: whoever may work tickets
there is who an unworked ticket escalates to. Written down here because it is a
choice the schema left open, not one it made.

### A shared clock nobody reset

`Later_rungs_still_fire_after_an_earlier_one_has` passed alone and failed in
the suite. The fixture is shared across every test class in the collection, so
a test that moves the clock leaves it moved; a test computing a due date from
"now" stays self-consistent, but one setting an absolute date does not. They
only fail together, in an order that depends on how the runner feels.

`TestClock.Reset()` now runs with the rest of the fixture reset. Worth
recording because the same shape exists in every fixture that carries mutable
state, and the failure it produces looks like flakiness rather than a bug.

**Verified:** 707 tests, 0 failures. Parity 1,371 objects across nine schemas.

**Documents updated:** none — no schema change.

---

## 2026-08-12 — Approvals that reach people, and two things a test found

Twenty-four tests. `ApprovalNotificationLog` and the stage timer columns have
existed since the schema was written; both are now used.

### The gap

`SubmitForApproval` resolved approvers, snapshotted them, activated the level —
and nothing ever told them. An approval waiting on somebody who does not know
is an approval that waits for ever. It could not be closed until Notifications
existed to ask through, and now it is: on submit, on each advance, on approval,
rejection and cancellation.

Every message goes through the outbox and leaves a row in
`ApprovalNotificationLog` saying why it was queued and what became of it. That
table is the answer to "nobody told me", which is the only question anybody
asks about an approval that went wrong.

### The reminder worker

`ApprovalReminderWorker` reads `IX_RequestApprovalStep_Due` — filtered on
Pending, precisely so this query stays small as finished runs accumulate — and
chases what has gone quiet. `ApprovalSchedule` holds the arithmetic on its own,
so the rules can be checked without a database, a clock or an approval.

**Occurrences are counted, not tracked.** A counter is state that can drift;
this is arithmetic that cannot. It also means a worker switched off for a day
comes back and sends the reminder due *now*, not the four it missed.

**Escalation goes up, not sideways.** The stage timer says when but not to whom
— unlike `SlaEscalation`, the approval schema has no recipient rule — so it
goes to whoever submitted the request. They asked for it, they are waiting on
it, and they are the one person certain to care that it has stalled. It happens
once: telling somebody hourly that a thing is still stuck is how they stop
reading it.

**Escalation suppresses the reminder in the same pass.** Somebody being
escalated over has already been reminded, and both in one minute is noise.

### Idempotency is derived, not random

`UX_ApprovalNotificationLog_Idempotency` only helps if the key is the same on
the second attempt. A random `Guid` would make every retry a new message —
exactly what the index exists to prevent — so `DeterministicGuid` derives it
from what the message IS: kind, instance, step, participant, occurrence. A
worker that restarts mid-pass collides instead of sending everybody their
approval request again.

### Two things the tests found

**A one-level route never told the sidelined approvers.** The handler announced
the outcome and returned, so on a route with a single stage the other approvers
of that stage were left with an approval in their list asking for a decision
that could no longer change anything. The step-approved notice now goes out
first and in every case, before the branch that ends the run.

**A participant can never lack an address**, so the `Skipped` path I had written
for one was unreachable. The resolver drops anybody without an e-mail at
submission — deliberately, so a level cannot end up waiting on somebody who
could not be asked — and `CK_RequestApprovalParticipant_Identity` requires the
snapshot to be non-empty. `Skipped` is real, but for the people looked up at
the time: the submitter. The test now exercises that, and the comment claiming
otherwise is corrected.

The second is the more useful kind of failure: the test did not find a bug in
the code, it found a sentence in a comment that was not true.

### Still not built

The SLA escalation monitor. It needs ticket data ServiceDesk owns and rules
ServiceLevel owns, so it needs a `ServiceDesk.PublicApi` contract first — a
piece of work of its own rather than a tail on this one.

**Verified:** 684 tests, 0 failures. Parity 1,371 objects across nine schemas.

**Documents updated:** none — no schema change.

---

## 2026-08-12 — Notifications, the thing that actually sends, and R3-10

Three tables, seven slices, a contract and a dispatcher. Thirty-five tests.
**Notifications is complete**, and it is the module every other one has been
queuing against without anything draining the queue.

### The outbox now has a second half

`EmailDispatcher` takes messages out, oldest first, sends them, counts the
attempts and gives up after enough of them. Until now `EmailOutbox` was a table
that grew.

`ServiceDesk.SendRequestEmail` was already writing a `RequestEmail` row and
leaving `EmailOutboxId` null. It now queues through `INotifier` inside the same
transaction (rule 4a) and keeps the id. Two rows rather than one, deliberately:
the `RequestEmail` row is the ticket's copy of the conversation, the outbox row
is the delivery attempt, and they answer different questions. A ticket whose
history vanished when somebody tidied a queue would be the wrong trade.

### R3-10 · Notifications had no capabilities at all

Not a gap in one screen — the module had none. Every e-mail in the system goes
through its outbox, and nothing could be granted to look at it, so a message
that failed to send was invisible to everybody. That defeats the entire point
of queuing rather than sending inline.

`email-setting.manage` and `outbox.manage` are seeded. **Sixth module in a
row** (R3-4 Assets, R3-5 Allocations, R3-6 Movements, R3-8 Transfers, R3-9
ServiceDesk, R3-10 Notifications).

There is deliberately no capability for reading your own notifications. Every
signed-in user reads their own, and a capability would be a lie: withdrawing it
would stop somebody being told things about their own work.

### The transport is a seam, and it earns its keep three times

`IEmailTransport` sits between the dispatcher and the mail server. The
dispatcher is testable without one, a site moving to a hosted mail API replaces
one class, and a development environment can log instead of sending to real
people. The tests use a transport that can be told to fail a set number of
times, which is the only reliable way to test the part that matters: what
happens when sending does **not** work.

Nine of the thirty-five tests are about failure.

### Rules the dispatcher keeps

- **No profile configured means nothing sent and nothing failed.** A site that
  has not set up SMTP yet has a queue that will send the moment it does;
  burning the attempt counter meanwhile would exhaust it before the first real
  try.
- **Every failure is the same failure.** A refused address and a host that is
  down both mean "not sent", and guessing which from an exception type is how a
  transient outage becomes a permanent one.
- **Requeue resets the attempt count to zero.** Somebody presses it because
  they have fixed what stopped the message; giving it one last try before it
  fails again would make the button useless in exactly the case it exists for.
- **A sent message cannot be requeued.** It would send twice and the person
  pressing the button would have no way of knowing.
- **A full batch skips the sleep.** Otherwise a backlog of a thousand drains at
  twenty every fifteen seconds — a quarter of an hour to say something that was
  urgent when it was queued.
- **The hosted service never throws.** A faulting `BackgroundService` takes the
  host with it by default, and losing the API because a mail server is
  unreachable would be a spectacular trade.

### Its own protector, not Identity's

`SmtpPasswordProtector` has purpose `AMS.Notifications.SmtpPassword`. docs/03
§8 says the purpose is part of the contract, and a purpose shared between two
kinds of secret means rotating a key for one breaks the other — an MFA
re-enrolment for every user because somebody changed an SMTP password would be
a memorable way to learn that. Each module storing a secret owns its protector.

A test writes a password through the settings slice and reads it back through
the dispatcher, over a real ephemeral key ring. A pass-through fake would have
proved nothing about that.

### The first logging in this codebase

`DispatcherLog` is source-generated `[LoggerMessage]` methods, because CA1873
objects to `logger.LogWarning(...)` with computed arguments and is right to:
the call evaluates and boxes them whether or not the level is enabled, and this
runs every fifteen seconds for the life of the process.

The pattern for anything else that logs: messages in one place per component,
with their levels and ids, so a reader can see everything a thing can say
without reading what it does.

### Still not built

The two workers this module was meant to unblock — approval reminders and SLA
escalation — are **not** in this change. `INotifier` is what they were waiting
for and it now exists; each belongs in its own module, next to the rules it
fires on, and each is a piece of work with its own tests.

**Verified:** 660 tests, 0 failures. Parity 1,371 objects across nine schemas.

**Documents updated:** `AMS_Consolidated_Design_v2.sql` §capability seed (R3-10).

---

## 2026-08-12 — ServiceLevel pass 2, and the null due dates finally filled in

Five slices, a calculator, and the contract that closes a loop opened two
modules ago. **ServiceLevel is complete** — 8 tables, 11 slices, 99 tests.

### The promise ServiceDesk made in pass 2

Its `RaiseServiceRequest` carried this comment:

> The clock starts when the ticket is raised. The due dates are the ServiceLevel
> module's to compute once a policy matches; until it ships they stay null.

They no longer do. `ISlaCalculator` is the third contract assembly, and
ServiceDesk now asks it two things:

| Question | Where the answer lands |
|---|---|
| What are this ticket's targets? | `SlaPolicyId`, `SlaStartOnUtc`, `ResponseDueOnUtc`, `ResolutionDueOnUtc`, `IsScheduledHold`, `NextOperationalStartUtc`, `ScheduleHoldReason` |
| How many operational minutes passed? | `ResolutionConsumedMinutes`, `SlaPausedMinutes`, `TechnicianWorkingMinutes` |

`SlaClock` no longer computes a duration. It decides which BUCKET an interval
lands in — running, paused, technician time — and takes the length as an
argument, because the length depends on the branch's working week and on
whether the policy respects it, neither of which is ServiceDesk's to know.

**"A ticket held over a weekend consumes nothing" is now true rather than
aspirational.** In pass 2 of ServiceDesk that phrase described bucketing;
it now describes the arithmetic as well.

### The clock starts when the branch opens, not when the ticket arrives

A ticket logged at ten at night gets `IsScheduledHold`, the opening time in
`NextOperationalStartUtc` — `CK_ServiceRequest_ScheduledHold` requires them
together, so they are written together — and a line in the timeline saying so.
A requester's first question about a ticket nobody is working on is why.

### The three Respect* flags are subtractive, and independent

Each one a policy turns off removes a reason the branch would otherwise be
shut. Implemented by editing the `CalendarSnapshot` before handing it to the
arithmetic, so `OperationalCalendar` stays a calculator that knows nothing
about policies:

- `RespectHolidays = false` → empty the holiday sets
- `RespectWeekends = false` → open Saturday and Sunday, **and clear the Saturday
  occurrence rules**, or "ignore weekends" would still close the second Saturday
- `RespectOperationalHours = false` → treat the branch as round the clock

A policy with all three off measures wall clock and never schedules a hold —
which is what a Critical policy usually wants, because a production outage does
not wait for Monday. A test holds that ignoring weekends still stops at closing
time; the flags do not imply one another.

### Smaller decisions

- **A policy's priority is not editable.** Moving one from High to Critical
  would change which tickets it judges without changing anything visible on a
  ticket, and every report spanning the change would measure two things under
  one name. The targets ARE editable, and the table is system-versioned so
  `FOR SYSTEM_TIME AS OF` can still read last quarter's.
- **No policy is an ordinary answer**, not a failure. A site that has not
  configured SLA still raises tickets; they have no due date, and a ticket with
  no due date is never overdue.
- **No policy still means wall-clock minutes**, not zero. Zero would leave such
  a ticket looking permanently untouched.
- **Thresholds have to climb.** Level 2 firing before level 1 is not a ladder,
  and the worker walks them in level order.
- **A recipient that is not Custom keeps no address**, even if one is sent —
  otherwise there is an address nobody writes to sitting in the row.
- **The ladder is deleted and saved before the new rows go in.**
  `UX_SlaEscalation_PolicyTypeLevel` is on (policy, type, level), and a delete
  and an insert of the same level in one batch collide: EF has no reason to
  order them.
- **`SlaPriority` is spelled again in ServiceLevel** rather than shared with
  ServiceDesk. Rule 2 — a constant one module reads out of another is a
  reference. The two agreeing is a fact about the design script, which both
  modules read.

### What is still not built

The monitor that walks `IX_SlaEscalationLog_Request`, fires the ladder and
writes `SlaEscalationLog`. Same reason as the approval reminder worker: it
belongs with Notifications and its outbox. Everything it needs exists and is
written correctly — including R2-3's filtered index, which lets a **failed**
attempt be retried while a Sent or Skipped row still blocks a repeat.

Two workers are now waiting on the Notifications module. That is the next
thing worth building.

**Verified:** 624 tests, 0 failures. Parity 1,329 objects across eight schemas.

**Documents updated:** none — pass 2 changed no schema.

---

## 2026-08-12 — ServiceLevel pass 1: the operational calendar, and a minute a day

Six slices and a calculator. Seventy-one tests, of which **38 touch nothing at
all** — no database, no clock. State a working week, ask what four working
hours from Friday afternoon means, check the answer.

That ratio is deliberate. This module is the arithmetic under every SLA due
date in the system, and an SLA report nobody trusts is worse than no SLA
report.

### Local time throughout, converted once at the edge

The snapshot's windows are wall-clock times at the branch; the instants
crossing `OperationalCalendar`'s edge are UTC. A branch opens at 09:00 where it
stands, and storing that as UTC breaks twice a year in any country with
daylight saving and permanently in any second country.

Two tests hold that: two branches in different zones open at different
instants, and a due date computed across the October clock change keeps its
local window while the instant moves.

`ILocationDirectory` is a third contract on Organization — a branch's time zone
and whether it exists. Nothing about its people; that is `IEmployeeDirectory`,
and a consumer needing one rarely needs the other.

### A round-the-clock day was 1,439 minutes

`TimeOnly` cannot hold 24:00, so a full day is stated as `MinValue` to
`MaxValue`. Read literally that is 23:59:59.9999999, and casting the span to
minutes truncated it: **a 24-hour branch lost one minute every day**.

I had written a comment claiming the opposite — that ending at `MaxValue` kept
the day at a full 1,440. It did not, and the test that asked directly said so.
`EndOf` now reads `MaxValue` as the following midnight.

It is the smallest bug in this codebase so far and among the more instructive:
invisible until an SLA report is a minute out and nobody can say why.

### Rules the arithmetic keeps

- **The window is half-open.** 18:00 is when the branch shuts, not its last
  open minute; a minute counted at both ends is counted twice.
- **A Saturday satisfies BOTH its weekday row and its occurrence row.** "We
  work Saturdays" and "we work the first and third" are different statements
  and a branch makes both — which is why the Saturday rules cannot collapse
  into the weekday table.
- **No Saturday rules at all means every Saturday follows the weekday row.** A
  branch that has not answered the question has not said no, so the slice
  stores nothing rather than five "not working" rows.
- **A 29 February recurrence is observed on 28 February** in years without one.
  The design script states it as an application rule; now it is one.
- **A target landing exactly on the closing bell has been met**, not missed by
  a night.
- **A branch that never opens gives no answer rather than spinning.** Every day
  marked non-working is a configuration mistake, but the walker cannot tell, so
  it is bounded at two years.

### The two intake rules are configuration, not code

"Raised in the final thirty minutes" and "raised on a Friday goes to Monday"
are columns a branch manager can turn off, and either can be ignored outright —
a Critical policy usually does, because a production outage does not wait for
Monday.

### Smaller decisions

- **A calendar arrives whole**: the standard window, all seven weekdays, the
  five Saturday rules. A half-saved calendar is one the SLA service would read
  and believe, and every due date computed in between would be wrong in a way
  nobody could see afterwards. Six weekdays is refused for the same reason.
- **A Standard day stores no times.** It means "whatever the standard window is
  now"; copying them in would leave seven stale copies the first time somebody
  edited that window. The read slice resolves them for display, so the screen
  shows what the day actually keeps.
- **An unconfigured branch gets Monday to Friday, 09:00 to 18:00** — and still
  observes its holidays. The default is about hours, not about pretending
  Republic Day is a working day.
- **A regional holiday attached to no branch is refused.** Observed nowhere
  looks exactly like working, and the stored `AppliesToAllLocations` flag exists
  precisely so the two mistakes cannot be confused.
- **The holiday year is derived from the date.** A client that could send both
  could send two that disagree, and `CK_HolidayCalendar_YearMatchesDate` would
  then 500.
- **No view capability.** The only screens that read a calendar are the ones
  that edit it; the arithmetic is called through a contract, not by a person.
  All three seeded capabilities were already there before a line was written —
  the first module for which that is true.

### Parity now covers eight schemas

`ServiceLevel` was added to `Compare-Schema.ps1`'s default list, which is the
list the fix two entries ago made real. 1,329 objects, matching.

**Verified:** 591 tests, 0 failures.

**Documents updated:** none — pass 1 changed no schema.

---

## 2026-08-12 — ServiceDesk pass 3, two new contracts, and a test that caught a rollback I was relying on

Eight slices: the routes and their versions, submit, My Approvals, the run,
decide, cancel. Forty-four tests. **ServiceDesk is complete** — 20 tables, 29
slices, 130 tests.

### Identity and Organization now publish contracts

Approver resolution needs to know who holds a role, who holds a capability at a
branch, and who somebody reports to. None of that is ServiceDesk's to read, so
rule 3 applies and two new contract assemblies exist:

| Assembly | Contract | Answers |
|---|---|---|
| `AMS.Modules.Identity.PublicApi` | `IUserDirectory` | who is this, who holds a role, who holds a capability (optionally at one branch) |
| `AMS.Modules.Organization.PublicApi` | `IEmployeeDirectory` | who does this employee report to, and where do they work |

Both are read-only, deliberately. Another module may need to write to somebody;
none of them may create a user or grant a capability.

`EffectiveAccess` carried a remark saying other modules never ask Identity
about capabilities. That was right about **authorisation** — the caller's own
capabilities are read from the token, resolved once at sign-in — and wrong as a
general statement, because approval routing has to address *the people who can
approve*. The remark is corrected and the new set-based query lives in that
same class, so "a deny beats a role grant" still has exactly one
implementation.

`ApproverResolver` is testable without an Identity database standing behind it,
which is the whole return on rule 3.

### The rules are the question; the participants are the answer

Approvers are resolved once, when their level's turn comes, and snapshotted
into `RequestApprovalParticipant` with name and address. A promotion, a rename,
a leaver: none of them may rewrite who was asked to approve something last
month.

Only the **first** level resolves at submission. Later levels resolve as they
activate, because a fortnight into a long approval "the requester's manager"
should be whoever that is then.

The same person reached by two rules is one approver. Asking somebody twice and
then waiting for both answers is a level that can never complete.

### A test caught me relying on the transaction

`SubmitForApproval` used to write the instance and its steps, then resolve, and
return an error if nobody was found — correct, because the dispatcher owns one
transaction per command and the error unwinds everything.

The handler tests call handlers directly, with no dispatcher. The run stayed
behind, and `A_route_that_resolves_to_nobody_does_not_start_a_run` failed on
exactly that.

The fix is not a transaction in the test. **Resolution now happens before a
single row is written.** The old shape was correct only because of something
outside the handler, and a run that started with nobody to ask would sit
Pending for ever if that ever stopped being true.

### Smaller decisions

- **A whole route arrives at once** — stages and their rules together — for the
  same reason a support team's membership does. An endpoint per stage would let
  a half-built route be picked up by a submission.
- **A route is created as a draft** and claims the single live-default slot at
  publication, not before. An unpublished default would take that slot
  (`UX_ApprovalWorkflowDefinition_OneActiveDefault`) from the route currently
  doing the job.
- **A version with approvals still running cannot be retired.** The runs
  survive either way — their steps are snapshots — but the administrator should
  find out first, not afterwards.
- **A rejection sinks the level under both modes.** A level exists to let
  somebody say no.
- **An optional approver cannot hold up an `All` level**, or `IsRequired` would
  be a decoration.
- **Settling a level closes out everybody who never answered.** Left Pending
  they sit in somebody's My Approvals asking for a decision that can no longer
  change anything.
- **`ClientDecisionId` makes a retry safe**, and the replay check runs before
  every other rule — a client asking whether its own earlier call got through
  should not be told "the level is finished".
- **The seven-branch `CK_ApprovalStageApproverRule_Value` is checked in the
  handler**, by name. The 500 it produces tells an administrator nothing about
  which field they left empty.
- **Nothing is deleted.** Cancelling marks the run Cancelled with who and why;
  R2-12's NO ACTION foreign keys make that a fact rather than an intention.

### Not built, and deliberately

The reminder, escalation and notification worker. `ApprovalNotificationLog`,
`IX_RequestApprovalStep_Due` and the stage timer columns all exist and are
written correctly, so the worker has everything it needs — but it belongs with
the Notifications module and its outbox, not here. Until it ships, an approval
nobody acts on stays Pending and nothing chases it.

**Verified:** 520 tests, 0 failures. Parity 1,180 objects across seven schemas.

**Documents updated:** none — pass 3 changed no schema.

---

## 2026-08-12 — ServiceDesk pass 2, the SLA clock, and reference data no migration ships

Nine slices: raise, My Requests, the queue, detail, assign, change status, note,
e-mail, attachment. Fifty-nine tests.

### Reference data lives in the design script, and the migrations do not carry it

The ticket statuses are seeded by section 17.2 of `AMS_Consolidated_Design_v2.sql`.
The EF migrations create `RequestStatus` and leave it empty — there is no
`HasData` anywhere in this codebase, by choice.

The test fixture had to seed the ten statuses itself before a single ticket
could be raised, and that is the finding: **a database built from migrations
alone cannot run the application.** No statuses, no capabilities, no asset
classes, no first admin user. Every one of those lives in the design script.

Nothing is broken today, because the design script is what builds a real
database and the parity check proves the two agree on *schema*. But schema
parity says nothing about data, and "run migrations" is what a deployment
guide would naturally say. This is now on the pre-live list beside the missing
first admin user.

### The clock is charged on the way out of a status, not on the way in

The design script says the `*Minutes` columns are operational minutes — "a
ticket held over a weekend consumes nothing". That is only true if something
charges time as it passes, and the only moments we reliably have are the
moments the ticket moves. So `SlaClock.Charge` closes the books on the interval
that just ended before opening the next one, and the bucket it lands in is
decided by the status being **left**, not the one being entered. Time spent On
Hold is paused time even though the move out of it is what discovers that.

Three consequences, each with a test:

- **A stopped clock is never overdue.** A ticket resolved yesterday does not
  become late tonight because its due date passed.
- **Time after resolution belongs to nobody.** Charging it would make reopening
  a ticket retrospectively blow an SLA it met.
- **The interval is clamped at zero.** `CK_ServiceRequest_SlaMinutes` rejects
  negatives, so a corrected system time shows up as a gap in the record rather
  than a failed status change.

### There is no transition table

Statuses are data — a site adds "Awaiting Vendor" without a release — so a
matrix of which may follow which would have to be maintained alongside them or
silently stop matching. What is enforced instead is what stays true whatever
the statuses are called: a ticket cannot move to where it already is, anything
that stops the clock needs its resolution written down, and the clock is
charged on every move.

For the same reason, **nothing is looked up by name**. "New" is the first
active status that is not a closed state, by display order; assigning a ticket
moves it to the next one after that. A site that renames Open to Logged keeps
both behaviours.

### A closed ticket takes nothing new

No note, no e-mail, no file, no reassignment. Reopen it first.

The closure is what the SLA report reads. A ticket that keeps accumulating
activity after it closed has a life outside its own recorded lifetime, and two
runs of the same monthly report then disagree depending on when they were run.
Reopening is one click and it appears in the history, which is the point.

### Smaller decisions

- **My Requests means raised BY me or FOR me.** A manager raises the joiner
  request and the joiner appears in the other column; both need to see it, and
  neither should have to know which column they are in.
- **A ticket with no due date sorts last.** SQL Server puts NULL first, which
  would float every ticket with no policy to the top of a queue ordered by
  urgency.
- **The overdue count is over the filter, not the page.** "3 of 50 overdue" on
  a page holding none is a number nobody acts on.
- **The template loses to the form.** A template fills in what was left blank
  and nothing else; one that overrode what the requester typed would be a form
  that argues.
- **An asset issue may name an asset that is not on the register.** The
  requester reads a sticker; refusing the ticket would lose the fault as well
  as the asset.
- **First response is stamped once**, by whichever of a public note, an
  outbound e-mail or a status change happens first. An internal note is not a
  response — that is the technician talking to the technician.
- **E-mail is written down, not sent.** Delivery is the Notifications module's
  through `EmailOutbox`, so a dead SMTP host retries instead of losing what
  somebody wrote. Status is `Queued`, and even `Sent` will only ever mean an
  SMTP server accepted it.

**Verified:** 476 tests, 0 failures. Parity 1,180 objects across seven schemas.

**Documents updated:** none — pass 2 changed no schema.

---

## 2026-08-12 — ServiceDesk pass 1, and a parity check that compared one schema

ServiceDesk is 20 tables — more than Allocations, Movements and Transfers
together — so it ships in three passes. This is pass one: the master data a
ticket refers to. Categories and sub-categories, support teams with members and
leads, service templates. Twelve slices, 27 tests.

### R3-9 · the ServiceDesk capabilities

Eight were missing from the seed: `request.raise`, `request.view`,
`request.manage`, `request.assign`, `request-category.manage`,
`approval-workflow.manage`, `approval.decide`, `approval.cancel`. They join the
five already there. Mirrored in `Modules/ServiceDesk/Capabilities.cs`, which is
the copy the endpoints actually name.

This is the fifth consecutive module whose capabilities were absent until its
slices were written. The pattern is now clear enough to state: **the seed is
written when the screens are, not when the tables are**, because until a screen
exists nobody knows what it needs permission to do.

### A team's membership is set as a whole, not one person at a time

`SetSupportTeamMembers` takes the entire set. Add/remove endpoints would make
the screen do arithmetic — read the list, diff it, send the difference — and
two people editing the same team would each apply their diff to a list that had
already moved. Sending the intended end state has no such window. Existing rows
keep their `AddedOnUtc`; duplicate user entries collapse with the lead flag
winning.

A team with members needs at least one lead, because escalation has to reach
somebody by name.

### Three rules the schema does not enforce, so the handlers do

- **The default team cannot be retired.** It is where routing sends what it
  cannot place; an inactive default means tickets with nowhere to go.
- **A template's sub-category must belong to its category.** The two columns
  are independent foreign keys, so nothing stops a template from pre-filling a
  ticket classified two ways at once.
- **A sub-category cannot be re-parented.** Moving it would silently reclassify
  every ticket already filed under it.

`RequestKind` is likewise not editable after creation: it decides which screen
a template appears on and whether approval applies.

### `CK_ServiceTemplate_Kind` allows three values, and I had guessed a fourth

Four tests failed on the CHECK. The vocabulary is `SupportTicket`,
`AssetIssue`, `NewService` — one pipeline carrying all three, which is why the
kind is a column and not three tables. Priority is `Low|Medium|High|Critical`.
Both are now constants in `Domain/ServiceDeskVocabulary.cs` and both are
validated in the handler, so a bad value is a 400 naming the allowed set rather
than a 500 from the database.

### The parity check was comparing one schema of six

`Compare-Schema.ps1` documents its `-Schemas` default as "every module that has
migrations". It was `@('Identity')`. Every run I had made passed the full list
explicitly, so a bare run — the one CI would make — would have checked 85
objects, ignored six schemas, and printed **MATCH**.

The default is now the real list, and adding a module means adding it there.

This is the third time in two days that a green check turned out to be green
about less than it appeared: the DEFAULT clauses the parser dropped, the
sequences nothing compared, and now the schemas nothing looked at. Same shape
every time — **"it says MATCH" means "nothing it looks at differs", not
"nothing differs"**, and each gap was found by code trying to use the missing
thing, never by the check.

**Verified:** 417 tests, 0 failures. Parity 1,180 objects across seven schemas.

**Documents updated:** `AMS_Consolidated_Design_v2.sql` §capability seed (R3-9).

---

## 2026-08-12 — Transfers, and a correction to yesterday's entry

**390 tests, 0 failures.** Parity **1,672 objects**.

### First: I documented four CHECK constraints and did not write them

The Allocations entry below says the status constants "spell what the database
allows". **They did not — `CK_AssetAllocationApproval_Status` and
`CK_AssetAcknowledgement_Status` did not exist.** The C# XML docs referenced
constraints that were never added, and the entry describing them was wrong
from the day it was written.

Either the docs were wrong or the schema was. The schema was: R2-7 gave the
Movements and Handover status columns a CHECK and missed these. **R3-7** adds
all four — the two in Allocations and two more in Transfers
(`_Status`, `_SapSyncStatus`). Parity went 1,668 → 1,672.

`AssetTransferRequest.TransferType` needs none of its own: the existing
`TypePair` CHECK already refuses anything outside the four, because none of its
branches can match another value.

### Two more contracts on `Assets.PublicApi`

- **`IAssetSnapshot`** — the read side, and the contract doc 01 §2 rule 3 names as its own example. Deliberately narrow: custody columns only, no finance, no custom fields. A snapshot that returned everything would become the way other modules read the register and the boundary would exist on paper only.
- **`IAssetCustody.ApplyTransferAsync`** — applies a completed transfer. Every argument optional, and a null means *leave it alone*, never *clear it*: a cost-centre transfer that also restated who holds the asset would silently undo an allocation made while it sat in the queue.

### The generator deleted three sequences

`HasSequence` was added to three DbContexts **by hand** yesterday. The
DbContext is a **generated** file, so the next `generate_model.py` run silently
removed all three and Movements stopped producing consignment numbers. The
whole Movements test file failed at once, which is the only reason it was
caught immediately.

This is the same lesson `write_once` already encodes for `ModuleExtensions`:
**a regenerated file cannot hold hand-written decisions.** So sequences are
generated now — `parse_design.py` reads `CREATE SEQUENCE` and fails loudly if
it matches fewer than the script declares, and `design-model.json` became
`{tables, sequences}` instead of a bare list of tables. That shape was itself
part of the problem: a list of tables cannot express a sequence, so the file
said the design had none.

**Three findings in two days now trace to the same root** — the parity check
and the generators only know what they have been taught, and "it says MATCH"
means "nothing it looks at differs".

### Transfers itself

Five slices behind one screen. A transfer is the **approval and the accounting
consequence**; the physical shipment it may cause is a separate movement.

- **Approving does not apply anything.** `transfer.approve` and
  `transfer.complete` are separate capabilities, so the person who wants a
  transfer cannot be the one who makes it true.
- **The "from" side is captured from the asset**, never supplied by the caller.
  A form that let somebody type where an asset came from is a form that lets
  them record a move that never happened.
- **Only the column the transfer is about changes.** Tested explicitly.
- **SAP is told about branch and cost-centre moves only.** Employee and
  department are AMS's own bookkeeping; queueing them would put thousands of
  rows in front of a system that discards them.
- **A completed transfer cannot be cancelled** — undoing it is a new transfer
  the other way, which is the only version of the story that stays true.

**R3-8**: Transfers had *no* capabilities seeded at all — the fourth module in
a row. Added `transfer.view/request/approve/complete`.

## 2026-08-12 — Movements, a second contract, and three sequences nobody had

**367 tests, 0 failures.** Parity now **1,668 objects** — three more than
yesterday, and that is the story.

### `IAssetCustody` — the second published contract

Receiving a shipment changes `Asset.CurrentLocationId`, which lives in
`[Assets]`. Movements may not touch it, so Assets publishes a second contract
beside `IAssetTimeline`. The assembly decided yesterday made this a five-minute
job instead of an architecture argument.

**There is deliberately no despatch call on it.** An asset in transit belongs
to neither branch, and the design script says why: marking it as arrived on
despatch makes it findable somewhere it is not. The branch changes once, on
receipt. `Despatching_does_NOT_move_the_asset` is the test that holds that.

### Compare-Schema was blind to sequences

The design script creates **three** — `RequestNumberSequence`,
`MovementBatchNumberSequence`, `ImportBatchNumberSequence`. **The EF model had
none**, and `Compare-Schema.ps1` had been reporting an exact match on 1,665
objects the whole time, because it compared columns, indexes, foreign keys,
CHECKs and defaults and never sequences.

The batch handler found it by trying to draw a consignment number and getting
*"Invalid object name"*. That is the expensive way round, and the second time
this exact shape of gap has appeared — the DEFAULT constraints were the first.

Fixed both ends: the three sequences are in the EF model, and the check now
compares `sys.sequences` on name, start value, increment and cycling.
`current_value` is deliberately excluded — it moves every time a number is
drawn, so comparing it would fail on any database anybody had used.

**The lesson worth keeping:** a parity check is only as good as its inventory,
and "it says MATCH" means "nothing it looks at differs", not "nothing differs".
Both gaps were found by code trying to use the missing thing, never by the
check.

### R3-6 — Movements had no capabilities of its own

Revision 2 seeded `handover.dispatch` and `handover.receive` — the two
*handover* steps — and nothing for the ordinary branch-to-branch despatch the
module is mostly used for. Third time: R3-4 in `[Assets]`, R3-5 in
`[Allocations]`, now here. Added `movement.view/manage/receive`.

`movement.receive` is split from `.manage` because receiving is the
**destination's** job. The person who despatched confirming their own arrival
is what makes a goods receipt worthless.

### Smaller decisions

- **One in-flight shipment per asset**, checked in the handler rather than by an index. There is no filtered unique index for it in the design, and adding one is a schema change; the check is honest about being a check. Two live shipments would mean whichever receipt landed second moved an asset that was already somewhere else.
- **The consignment number comes from the sequence, not MAX+1.** Two branches despatching at the same moment would both read the same maximum, and `UX_MovementBatch_Number` would then reject one of them for no reason a user could act on.
- **It is read with a direct `DbCommand`, not `SqlQuery<T>`** — EF wraps that in a subquery, and `NEXT VALUE FOR` is illegal inside one.
- **A batch closes by counting what is still out**, not by decrementing a counter. A receipt that rolls back cannot then leave the count wrong.
- **The GRN queue is oldest first.** It exists to be worked, and something despatched three weeks ago that never arrived is the row worth chasing — newest-first would bury it under this morning's parcels.

## 2026-08-12 — Contract assemblies, and the silent bug the first consumer found

Allocations is the first module to call another module's contract, and it
exposed that the mechanism did not exist.

**344 tests, 0 failures.** Parity unchanged at 1,665 objects.

### The rule contradicted itself

Rule 2 forbids a module referencing another module, and the architecture test
enforces it from `.csproj` files. Rule 3 says to *"depend on its PublicApi
contract instead"*. But `IAssetTimeline` lived inside
`src/Backend/Modules/AMS.Modules.Assets/PublicApi/` — inside the project nobody may
reference. **The two rules could not both be obeyed.** It had not bitten
because Assets was the only module with slices and it used its own contract.

### Decided: one contract assembly per publishing module

`AMS.Modules.Assets.PublicApi` — the interface and its DTOs, referencing
`AMS.SharedKernel` and nothing else. The implementation stays in the module.

**Rejected: a single shared `AMS.Contracts`.** Simpler today, worse at fifteen
modules. Everything would reference one assembly holding every contract, so
*"which modules does this one depend on"* stops being answerable from the build
— the exact question schema-per-module exists to keep answerable — and a
project everyone already references becomes where the next awkward thing goes.
`AMS.SharedKernel`'s own `.csproj` warns about that failure mode by name.

Per-module assemblies keep the dependency graph readable in one file, and are
the seam if a module ever becomes a service.

**Named `.PublicApi`, not `.Contracts`** — because there is already a business
module called **Contracts** (maintenance and AMC), so that suffix could not
tell `AMS.Modules.Contracts` from a contract assembly. *The architecture test
written minutes earlier caught it on its first run.* `.PublicApi` is also the
word doc 01 rule 3 already used.

### The boundary rules, updated

- `No_module_references_another_module` → `No_module_references_another_modules_implementation`. Contract assemblies are permitted; module projects are not.
- New: `A_contract_assembly_carries_no_implementation`. A contract assembly may reference `AMS.SharedKernel` and nothing else — no EF, no ASP.NET, no FluentValidation. One that grew an EF dependency would drag it into every consumer and quietly rebuild the coupling the split removes.
- The fifteen-modules test now excludes `.PublicApi` projects from the count.

### The silent bug: `IAssetTimeline` never wrote anything

Three Allocations tests failed, and they were right to.

`AppendAsync` staged the row and left saving to *"the calling handler's
transaction"*. That works **only** while the caller is inside Assets and holds
the same `AssetsDbContext`. From any other module — the entire point of the
contract — the caller saves a *different* context and **the timeline row is
silently dropped**. The allocation commits; its history never existed.

That is the failure the design calls worse than no timeline, reached from the
opposite direction, and it was written into the contract's own comment as a
virtue.

`AppendAsync` saves its own context now. Saving is not committing: rule 4a puts
every module context on one transaction owned by the dispatcher, so a failed
command still takes its timeline row with it —
`A_failed_change_takes_its_timeline_row_with_it` still passes and is the proof.

The original reasoning was written before the unit of work existed. It was
correct then and wrong afterwards, and nothing re-read it.

### R3-5 — Allocations had no capabilities of its own

Revision 2 seeded `handover.record` and `allocation.revert-return` and nothing
else, so every Allocations screen would have declared a capability no
administrator could grant. Same gap as R3-4 in `[Assets]`. Added
`allocation.view/manage/request/approve`, `acknowledgement.approve` and
`customer-site.manage`, split by audience — `allocation.approve` is separate
from `.manage` because the point of raising a request rather than allocating
directly is that *somebody else* decides it.

### Also: two status columns had no CHECK

`AssetAllocationApproval.Status` and `AssetAcknowledgement.Status` carried
their vocabulary in a comment only, unlike Handover and ServiceRequest which
R2-7 gave CHECKs. The C# constants in `AllocationVocabulary` spell what the
database allows, and `CA1720` (which objects to `Signed`) is disabled centrally
with the reason: renaming a constant to satisfy an analyzer would make the C#
and the CHECK disagree.

## 2026-08-12 — The API composition root: the slices are now reachable

`Program.cs` was four lines returning "Hello World!". Seventy tested slices,
none of them served. This wires them: `SystemClock`, `ICurrentUser` from claims,
a dispatcher with one transaction per command, capability policies, JWT
issuing and validation, and health endpoints.

**315 tests, 12 of them booting the real host over HTTP.** Parity unchanged at
1,665 objects.

### A fourth kernel project, for the same reason there was a third

`AMS.SharedKernel` says in its own `.csproj` that it must reference **nothing**.
The unit of work needs `DbContext` and `DbConnection`; the web kernel has
ASP.NET but not EF; and modules may not reference the host or infrastructure.
So `AMS.SharedKernel.Persistence` exists — the same split, for the same reason,
as `AMS.SharedKernel.Web`.

### Rule 4a is now a mechanism, not a note

`UnitOfWork` holds **one `SqlConnection` per request**; every module context is
built on it via `AddModuleDbContext` instead of a connection string. The
dispatcher opens one transaction per command. GateB proved this works; this is
it wired into every request.

Enlistment is an interceptor, not a line in each handler, because a handler
that forgets does not throw — it commits half a command while the rest rolls
back, which is the exact failure the design is arranged to prevent.

### `IPersistsOnFailure`, and why the exception is explicit

Commands roll back on failure. `SignIn` increments the failed-attempt counter
and *then* refuses, and its own remarks say a rollback there "would hand an
attacker unlimited guesses". Rather than weaken the rule for everybody, the
command declares itself: `IPersistsOnFailure`, one implementation, and a test
that five wrong passwords still lock the account.

### Three defects only booting the host could find

1. **`DELETE /assets/{id}` inferred a request body.** Minimal API refuses that,
   and endpoint building fails *for the whole application* — every route
   returned 500, including `/health/live`. The reason is a query parameter now.
2. **`UnitOfWork` implemented only `IAsyncDisposable`.** The container throws
   when anything disposes its scope synchronously, which is what every
   background job and EF tooling entry point does.
3. **The enlistment interceptor only hooked `SavingChanges`.** A handler's first
   statement is a read, and a read on a connection with a pending transaction
   is refused outright — so every request died before reaching a save. It hooks
   commands as well now.

None of these was reachable from a handler test. That is the argument for
`AMS.Api.Tests` existing at all.

### Validation moved to the HTTP edge

The dispatcher validated the **Command**; every validator in this solution
targets the **Request**. So validation silently never ran — a 400 case
returned 201. It is an endpoint filter on each module's group now, which is
also the only place the Request exists.

### Consequence worth knowing

Capabilities travel **in the token**, resolved once at sign-in. That is what
keeps `[Identity]` out of the path of every request in every other module
(rule 2). The cost: a capability change reaches somebody when their next token
is issued, not instantly. Locking an account is the case that cannot wait, and
lockout is checked at sign-in, so a locked user cannot get a new token.

`EffectiveAccess` is shared by the sign-in path and `GetUserCapabilities` on
purpose: the query is what an administrator reads off the screen to check
somebody's access, and two copies of "a deny beats a role grant" is two chances
for that screen to be a lie.

## 2026-08-12 — The Assets master-data screens, and a silent write bug they found

Fifteen slices: asset types (with the seven behaviour flags), asset classes,
chart of accounts, statuses, and custom fields. **69 Assets tests, 249 in the
suite, all passing.** Parity still exact at 1,665 objects.

### R3-4 — the register had no capabilities of its own

Revision 2 seeded `field-asset.view` and `field-asset.manage` for `[Assets]`
**and nothing else**. Every Asset screen would have declared a capability no
administrator could grant, so the screen would simply have been unreachable —
the failure Identity found the hard way and R2-24 exists to prevent.

Added `asset.view`, `asset.manage`, `asset-taxonomy.manage` and
`asset-finance.view`, seeded in the design script and mirrored in
`Assets/Capabilities.cs` **in the same change**. Split three ways because the
catalogue splits the *audience* three ways, not for symmetry: running the
register is a branch job, inventing an asset class is not, and book values are
read-only everywhere because SAP owns them — which is why `asset-finance.view`
has no matching `.manage` and should never get one.

### A defaulted column could not be set to its CLR default

`The_seven_behaviour_flags_survive_a_round_trip` failed:
`IsAllocatable = false` came back `true`.

EF treats a column with `HasDefaultValueSql` as **store-generated**, so it omits
that column from the INSERT whenever the property still holds the CLR default.
`false` *is* the CLR default for `bool`, and the column defaults to `1`, so the
value was dropped on the way to the database and the row came back allocatable.

**Seven booleans and two integers** in this design behaved that way. The
integers are the ones that would have hurt quietly:
`LocationOperationalHour.DeferFinalMinutes` and `SlaPolicy.
NearDueWarningMinutes` both default to 30, and `0` — *no deferral*, *no warning*
— is a legitimate setting that silently became 30.

Fixed in `generate_model.py`, generally rather than per column: any non-nullable
column with a DEFAULT that maps to a C# literal now gets **the same default on
the entity** and **`ValueGeneratedNever()`** on the property, so EF always sends
what the caller asked for and the database default is left to do its real job —
serving the design script and any importer writing raw SQL. Initializers are
emitted only where the literal differs from C#'s own default, because CA1805 is
right that `= false` says nothing.

Proven by the fifteen migrations this generated: **all fifteen were empty.** The
mapping changed; the schema did not.

### Not fixed: `has-pending-model-changes` reports every module as pending

`dotnet ef migrations has-pending-model-changes` says all fifteen modules have
pending changes. Adding a migration does not clear it — the fifteen it produced
were empty and the report was unchanged afterwards, so they were removed again.
It reported the same before any of this work.

It is not a real difference: `dotnet ef database update` applies every module to
a fresh database without complaint, and `Compare-Schema.ps1` matches exactly at
1,665 objects. **Trust the parity check, not this command.** Recorded here so
the next person does not spend an afternoon adding empty migrations to silence
it, as this session briefly did.

---

## 2026-08-12 — Revision 3 LANDED in the design script, and four things it uncovered

Section 3 rewritten, Section 16 deleted, seeds added. The script runs clean on
an empty database: **15 schemas, 86 module tables at the Section 18 checkpoint
(91 rows in `sys.tables` with the five history tables), 94 / 99 once the
approval-workflow extension has run.** `Compare-Schema.ps1` reports **1,665
objects, exact match** across all fifteen schemas.

Thirteen constraint probes were run against a freshly built database — the
script is at `build/Test-DesignConstraints.sql`, re-runnable. The two that
matter are the ones that would have caught a *wrong* design rather than a typo:
the same bulk line counted at two branches is **accepted**, and a unit asset
sighted twice in one cycle is **refused (2601)**.

### 1. Assets is **18** tables, not 17

[`07ASSETREGISTERDESIGN.md`](07ASSETREGISTERDESIGN.md) §9 said "10 → 17". Its
own list has eight additions — `AssetClass`, `ChartOfAccount`, `AssetFinance`,
`AssetDepreciationEntry`, `AssetHolding`, `AssetDisposal`,
`AssetVehicleDetail`, `AssetInstrumentDetail` — and 10 + 8 = 18. The rename of
`AssetCategory` to `AssetType` is not an addition and was probably counted as
cancelling one out. The script has 18; the doc has been corrected.

### 2. `Asset.ImportBatchId` — a deviation from the design doc (R3-2)

§7 of the design doc maps **13 of `FieldAsset`'s 14 columns** to a richer home.
The fourteenth, `ImportBatchId`, has none: it was the only row-level link to
`[DataImport]` anywhere in the design, and `ImportBatch` has no per-row lineage
table of its own. Folding the module in as written would have *lost* it.

Added to `[Assets].[Asset]` as a nullable id-only column with a filtered index,
so a 7,413-row register import can still be traced back to the batch that
created each row. **This is an addition to Fable's Revision 3, not a reading
of it.**

### 3. `parse_design.py` was silently dropping DEFAULT constraints

The default-clause regex required exactly one space between `]` and `DEFAULT`.
The DDL aligns its DEFAULT clauses into a column, so every constraint name
shorter than the longest one in its table is followed by padding — and only the
longest one in each table matched. **12 of 51 defaults never reached the EF
model.**

This is not cosmetic. `[AssetType].[IsAllocatable]` is `NOT NULL` with no CLR
default, so without its `DEFAULT (1)` an ordinary insert fails; and
`[Asset].[Quantity]` losing `DEFAULT (1)` would have made `CK_Asset_Quantity`
`Positive` unsatisfiable for every `new Asset { ... }` in the codebase.

`generate_model.py` had a second, unrelated defect in the same area: the
`ConcurrencyStamp` branch `continue`d before reading the column's default, so
all five `newid()` defaults were dropped regardless of the regex.

### 4. `Compare-Schema.ps1` never compared DEFAULT constraints

It compared columns, indexes, foreign keys and CHECKs. It reported **"1,464
objects, exact MATCH"** while those 12 defaults were missing, because defaults
were not in the inventory at all. Added `sys.default_constraints`, which is how
the twelve were found.

Naming them turned out to matter too: without a name SQL Server invents one
(`DF__AssetType__IsAll__395884C4`) that differs on every database, so the two
sides could never be compared on equal terms. `generate_model.py` now emits
`HasDefaultValueSql(sql, name)`.

EF scaffolds a default-constraint rename as `AlterColumn`, which rewrites the
column and **fails outright when an index depends on it** — nine columns here
sit under a filtered index. `build/rewrite_default_migrations.py` replaces
those migrations with `sp_rename` (or `ALTER TABLE ... ADD CONSTRAINT` where no
default existed at all), which needs no column change.

### 5. `generate_model.py` was destroying each module's composition root

Re-running the generator overwrote `OrganizationModuleExtensions.cs` and took
**24 handler registrations, 24 endpoint mappings and 8 unique-index
`SqlErrorTranslator` entries** with it. Caught by reading the staged diff, not
by the suite: every Organization test passed, because they call handlers
directly rather than resolving them from DI.

`ModuleExtensions` and `DbContextFactory` are **scaffolds** — every slice adds
lines to them — while entities, configurations and the `DbContext` are
**mirrors** of the design script and hold no hand-written decisions. The
generator now writes the two scaffolds only when they do not already exist and
prints `(kept: ...)` for each one it leaves alone. Verified by re-running it
against the restored file.

This is the risk the generator's own header warns about — "Regeneration is for
the initial import only; after that the files are ordinary source" — left as a
comment rather than enforced. It is enforced now. **The generator is safe to
re-run, and every developer and tool in this repository should assume it will
be re-run.**

### Also fixed: an MFA test defect that was time-bombed, not flaky

Seven `VerifyMfaCodeTests` and one `MyProfileTests` were failing **before any
Revision 3 change** — verified by stashing the whole branch and re-running.

`MfaChallengeTokens` stamps a token's expiry from its injected `IClock`, but
`ITimeLimitedDataProtector.Unprotect` judges that expiry against the **real**
system clock and offers no way to override it. The fixture handed it the frozen
test clock, so every token expired at 09:05 on 12 Aug 2026 regardless of when
the suite ran: the tests passed for five minutes after that instant and would
have failed forever afterwards. The fixture now issues challenges with a real
clock. In production both are the same clock, which is why this only ever bit
the tests.

**Full suite: 186 passed, 0 failed.**

### Open, and NOT fixed here

`IClock` has **no production implementation and no DI registration** anywhere
in `src/`. Every handler takes one, so the API cannot resolve its dependencies
today. This is outside Revision 3 and is flagged rather than fixed silently.

---

## 2026-08-12 — AMENDMENT · the asset model is wider than IT

**Status: the three blocking decisions are ANSWERED (below). The design in
[`06ASSETMODELREVISION.md`](06ASSETMODELREVISION.md) is being revised to match
before implementation.**

### The decisions

**1. SAP owns depreciation. AMS mirrors it, read-only.**
`AssetFinance` and `AssetDepreciationEntry` are populated by the SapSync
module and are never written by a user or by an AMS calculation. There is no
depreciation run, no month-end job, no posting lifecycle and no reconciliation
problem, because there is only one system doing the arithmetic. AMS shows net
book value on the asset screen; it does not compute it.
*Consequence:* the finance tables need a `LastSyncedOnUtc` and a sync
watermark, and every finance field is read-only in the API. If this ever
flips, the tables stay and a calculation job is added — the shape does not
change, only who writes it.

**2. Quantity is hybrid: serialised assets are one row, bulk lines carry a
quantity.**
`AssetCategory.IsSerialised` decides. A laptop or a vehicle is one row with a
serial number, allocatable and individually verifiable. Chairs and barricades
are one line with `Quantity = 1163`, tracked in bulk.
*Consequence, and it is the important one:* **allocation, handover and physical
verification apply only to serialised rows.** A bulk row is moved by quantity,
not issued to a person. This must be enforced, not documented — see the
follow-up below.

**3. `FieldAssets` folds into the main register.**
Site equipment becomes a category with `TracksSiteDeployment = 1` and an
`AssetSiteDeployment` detail row, gaining allocation, movements, the timeline,
verification, contracts and custom fields. The `field-asset.*` capabilities
stay and now scope a view of one register instead of gating a second one.
*Consequence:* the `FieldAssets` module and schema are removed. Sixteen modules
become fifteen. `docs/01` §1, the design script Section 16, and the catalogue's
module and feature lists all change.

### The follow-up this created, and how Revision 3 answers it

I flagged that "an allocation may only reference a serialised asset" could not
be enforced, because the flag lived on the category in another schema.

Revision 3 solves it more cleanly than the denormalised flag I expected:
`IsBulk` and `Quantity` sit on `Asset` itself, and
**`CK_Asset_UnitQuantityIsOne`** (`IsBulk = 1 OR Quantity = 1`) makes "every
allocatable asset has Quantity = 1" a *proof*, in the database, with no
cross-schema reach at all. Allocation, handover and unit verification never
have to reason about quantity. `CK_Asset_BulkNotHeld` forces bulk custody
through place-level `AssetHolding` rows, because a bulk line is in four places
at once and has no single current location.

The one genuinely new mechanism is the **split**: to issue 20 barricades to a
person you carve a unit asset out of the bulk line (`SplitFromAssetId`), and
allocate that. One mechanism instead of a dual-mode allocation table.

**What forced it.** `docs/Fujitec India- FAR as on 18-07-2026.xlsx` is the live
fixed asset register: 7,415 assets, 64 columns. IT is **1,834 of them — 24%**.
Furniture & Fixtures alone is 2,181. The `Assets` schema was designed for the
minority: `Asset.Hostname` on the core row, and 1:1 detail tables holding
processor, memory, MAC and IP.

**What the schema cannot represent at all:**

- depreciation and book value — method, percentage, useful life, opening and
  closing accumulated, charged for the year, net book value
- quantity — `Capitalized Qty`, `Disposal Qty`, `Gross Qty`; a row is not
  always one thing and partial disposal is normal
- chart of accounts — three code/description pairs per asset
- insurance policies, disposals, vouchers, GRN, useful life, project linkage

**And one taxonomy where the business runs three:** Asset Category (9), Asset
Class (13, accounting), TechnicalGroup (342 — Chairs, Dell Laptop, Tool Kit).

**Blocked on:**

1. Does AMS own depreciation, or does SAP remain the system of record?
2. Is one row one thing, or one line with a quantity? This decides whether
   allocation, movement and verification must handle partial quantities, which
   is a far larger change than adding tables.

**Unaffected and already built:** the `IAssetTimeline` write contract, and the
Identity and Organization modules. The timeline contract is deliberately
type-agnostic and survives any answer here.

---

## 2026-08-12 — `ICurrentUser.EmployeeId`

**Deviates from:** the original `ICurrentUser`, which carried only the user id.

**Why.** "See my application access" needs to know which *employee* the caller
is. That link is `Identity.User.EmployeeId`, and Organization may not read
another module's table (`01` §2 rule 2). Every "my ..." screen in the
catalogue has the same problem — my assets, my tickets, my approvals.

**Decision.** Resolve it once at authentication and carry it as a claim.
`int? EmployeeId` on `ICurrentUser`. **Null is normal**: a service account or
an administrator who is not in the directory has a login and no employee
record, and a screen showing somebody their own things must be able to say so
rather than showing an empty list as though they had none. A test covers that
case.

**Documents updated:** this file. `01` §2 is unchanged — the rule held; the
claim is how it holds.

---

## 2026-08-12 — R2-25 · Organization capabilities seeded

`organization.manage`, `organization.view` and `application-access.manage`. One
capability covers branches, regions, departments, vendors and the application
master: the same job, done by the same person, and splitting it would produce
five grants nobody ever sets differently.

Seeded at the same time as the code that declares them, per R2-23's lesson —
an endpoint declaring a capability the seed lacks can never be granted to
anybody, so the screen is simply unreachable.

---

## 2026-08-12 — EF's index-per-foreign-key convention is removed

**Deviates from:** EF Core's defaults, not from the design.

**Why.** EF adds an index for every foreign key by convention. Against the
reviewed script that produced **14 indexes nobody asked for** — on
`ServiceRequest`, `ServiceTemplate`, the approval tables and others. The design
adds an index where a query needs one (`IX_UserRole_RoleId`,
`IX_RoleCapability_CapabilityName`, both marked "FK support" in the script) and
leaves it out where nothing reads that way. Every extra index is a write cost
on a table somebody already measured.

**Decision.** All sixteen contexts remove `ForeignKeyIndexConvention`, so the
model is the script and nothing else.

**Evidence.** `build/Compare-Schema.ps1` — 1,463 objects, exact match across
all sixteen schemas.

---

## 2026-08-12 — Integer primary keys that are not IDENTITY

**Deviates from:** EF Core's defaults.

**Why.** EF treats an integer key as `IDENTITY` by convention. Several tables
here take their key from another module — `Discovery.AssetHealth` is one row
per asset, keyed by the Assets module's id — so the value arrives with the row
and the database must not invent one. The parity check caught
`Discovery.AssetHealth.AssetId` becoming an identity column.

**Decision.** The generator emits `ValueGeneratedNever()` for any single-column
integer key the script does not declare `IDENTITY`.

---

## 2026-08-12 — Rule 4 clarified: `*Utc`, not necessarily `*OnUtc`

**Deviates from:** a strict reading of `03` §1 rule 4.

Three columns end in `Utc` without `OnUtc`: `EffectiveFromUtc`,
`EffectiveToUtc`, `NextOperationalStartUtc`. Inserting "On" would make each one
worse English for no gain. The rule's purpose — a reader can tell it is UTC
without opening the schema — is satisfied.

**Decision.** Any `*Utc` suffix satisfies rule 4. `*OnUtc` remains the norm and
what almost every column uses. Enforced by
`PersistenceConventionTests.DateTime_properties_are_named_Utc`.

---

## 2026-08-12 — The model is generated, once

`build/parse_design.py` reads the design script; `build/generate_model.py`
writes the entities, configurations and contexts. 87 tables mirrored by hand is
87 chances to mistype a max length.

**These are persistence-faithful entities, not a domain model.** Behaviour is
added by hand as slices are built, and **nothing should be regenerated over the
top of it**. Regeneration was for the initial import; the files are now
ordinary source. Identity was written by hand first and is deliberately skipped
by the generator.

Two parser bugs are worth knowing about, because both produced output that
looked complete:

- A table body ends at `);` **or** at `) WITH (SYSTEM_VERSIONING = ON (...));`.
  Handling only the first form made every temporal table swallow the table that
  followed it — 82 of 87 tables, no error.
- Line comments must be stripped **before** splitting a body on commas.
  `-- NEW  Allocations.AssetHandover, id only` contains one, which tore a
  column in half and made `AssetEvent` appear to have no primary key.

---

## 2026-08-12 — `AMS.SharedKernel.Web`, a new project

**Deviates from:** `01` §1, whose project list has no such project.

**Why.** Every module's `*Endpoint.cs` needs `ToHttpResult` and
`RequireCapability`. Those helpers touch ASP.NET Core, and there was nowhere
legal to put them:

- **`AMS.SharedKernel`** must reference *nothing* — the moment it takes an
  ASP.NET dependency, all sixteen modules and every unit test inherit it.
- **`AMS.Infrastructure`** is forbidden to modules by `01` §2 (dependencies
  point inward), and the architecture test enforces that.

**Decision.** A small `AMS.SharedKernel.Web` carrying a `FrameworkReference`
to `Microsoft.AspNetCore.App`, which modules *are* permitted to reference. It
holds HTTP concerns only and no business logic.

**Documents updated:** `01` §1 project tree · this file · the architecture test
carries the exception in a comment beside the rule.

---

## 2026-08-12 — R2-23 · Identity capabilities added to the seed

**Deviates from:** Section 17.6, which seeded capabilities for **new** features
only.

**Why.** The reference slices declare `user.manage` and `user.view`, and an
endpoint declaring a capability that is not seeded can never be granted to
anybody — the screen is simply unreachable. The existing screens have always
needed these names; nothing had written them down.

**Watch for this in the other fifteen modules.** Every capability an endpoint
declares must exist in the seed. `02` §10's checklist already says so; this is
the first time it bit.

**Documents updated:** design script Section 17.6.

---

## 2026-08-12 — R2-22 · `ConcurrencyStamp` replaces `SysStartTime` as the concurrency token

**Deviates from:** R2-1, which nominated `SysStartTime` for the five
system-versioned tables (`Employee`, `Asset`, `Contract`, `SlaPolicy`,
`LocationOperationalHour`).

**Why.** R2-1's premise was that `SysStartTime` "is regenerated on every
UPDATE". Measured against SQL Server 2022, it is not. The period start is
stamped from the **transaction start time**, and the Windows system clock
advances in ticks of roughly 1–15 ms, so two updates inside one tick receive
the same value. 20 of 20 insert-then-update pairs left it unchanged; a 50 ms
delay changed it every time.

The consequence was a **silent lost update**: the second writer's stale token
still matched, their `UPDATE` affected one row, and no error was raised
anywhere. Zero-duration versions are not retained either, so those edits left
no history row to appeal to.

**Decision.** Add `[ConcurrencyStamp] uniqueidentifier NOT NULL DEFAULT
(NEWID())` to the five tables and map that as the token. The audit interceptor
re-generates it on every update. `SysStartTime` returns to doing only what it
is for — history and `TemporalAsOf`.

**Cost.** 16 bytes per row on five tables. A schema revision before release, so
no migration of live data.

**Evidence.** `src/Backend/tests/AMS.PersistenceGates.Tests/GateA_TemporalConcurrency.cs`,
particularly `Test1b`, which reproduces the lost update rather than hiding it.
Enforced by `PersistenceConventionTests`.

**Documents updated:** `03` §1 rule 7, §4 · design script header R2-22 and all
five table declarations.

---

## 2026-08-12 — R2-21 · `ClientCaptureId` on `PhysicalVerification`

**Deviates from:** the original Section 10, which had no client idempotency key.

**Why.** The audit is captured offline and retried, so a lost response and a
request that never arrived are indistinguishable to the phone. With only
`UX_PhysicalVerification_OnePerAssetPerCycle`, the server had to answer every
retry with a conflict — which teaches technicians to ignore conflicts.

**Decision.** `[ClientCaptureId] uniqueidentifier NULL` plus a filtered unique
index. A phone resending its own capture now gets the existing row and a 200;
only a second technician verifying the same asset is a 409.

**Documents updated:** `05` §4 and §7 · `03` §7 error-translation table.

---

## 2026-08-12 — R2-20 · `WorkingCondition` CHECK constraint

**Deviates from:** Section 10, where the column had no CHECK — the same defect
R2-18 fixed on `ServiceRequest.RequestKind`.

**Decision.** Reuse the `AssetHandover.ReturnCondition` vocabulary
(`Good | MinorDamage | Damaged | NotWorking | Missing`) rather than invent a
second one. "Damaged" on a return and "Damaged" on an audit should be the same
word, and an audit needs to be able to say "Missing".

**Open.** If the business wants a distinct audit vocabulary, change it in the
script first; the mobile app mirrors this list and does not define it.

**Documents updated:** `05` §5 · design script header R2-20.

---

## 2026-08-12 — R2-18, R2-19 · Missing CHECK, and a misleading verification count

`ServiceRequest.RequestKind` had its allowed values in a trailing comment only,
on the column the entire approval extension keys off. Section 18's table count
ran before the approval extension built its 8 tables and reported the result as
the total.

**Documents updated:** design script header.

---

## 2026-08-12 — Rule 4a · The transaction spans modules; the DbContext does not

**Deviates from:** nothing — it *completes* rule 4, which was unimplementable as
written. `AssetEvent` lives in `[Assets]` and `EmailOutbox` in
`[Notifications]`, so a handler could not write either through its own module's
context while one DbContext maps one schema.

**Decision.** Every module context is built on one shared `DbConnection`;
`UnitOfWorkBehavior` opens one transaction per command and enlists each context
it resolves. Cross-module writes go through the owning module's `PublicApi`
write contract. No MSDTC.

**Evidence.** `src/Backend/tests/AMS.PersistenceGates.Tests/GateB_CrossModuleTransaction.cs`
— two contexts commit together and roll back together.

**Documents updated:** `01` §2 rule 4a and §4 · `02` §4 · `03` §2.

---

## 2026-08-12 — Rule 7a · `RowVersion` is never nullable

Mapping it `byte[]?` produces a **nullable** column, against R2-14's NOT NULL.
Caught by the schema-parity check on its first run against Identity.

**Enforced by:** `PersistenceConventionTests.RowVersion_is_never_nullable`.

**Documents updated:** `03` §1 rule 7a.

---

## 2026-08-12 — Analyzer decisions that overrule documented samples

The build treats analyzer violations as errors, and three of them disagreed
with shapes the standards prescribe. Each is resolved once, centrally, in
`.editorconfig`, with the reason beside it:

| Rule | Decision |
|---|---|
| `CA1000` | off — `Result<T>.Success()` is the documented factory shape (02 §3) |
| `CA1716` | off — the type is named `Error` in 02 §3 and in every handler signature; the reserved keyword is VB's |
| `VSTHRD103` | demoted — it flags `DbSet.Add` and demands `AddAsync`, which EF's own guidance says not to use |

Two documented **samples** were wrong and were corrected rather than
suppressed: `OnModelCreating(ModelBuilder b)` and
`Configure(EntityTypeBuilder<T> e)` both violate `CA1725`, which requires an
override to keep the base member's parameter names. A sample that fails the
build teaches the wrong thing.

**Documents updated:** `03` §2 and §3.

---

## 2026-08-12 — `InvariantGlobalization` must stay off

Set `true` in `Directory.Build.props` for size, it makes
`Microsoft.Data.SqlClient` throw *"Globalization Invariant Mode is not
supported"* the moment it opens a connection. The API would have built clean,
started clean, and failed on its first query. Everything here talks to SQL
Server.

**Found by:** the persistence gates, on their first run.
