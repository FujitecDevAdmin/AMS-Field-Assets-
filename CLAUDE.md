# AMS

**Read [`AGENTS.md`](./AGENTS.md) first.** It is the single set of instructions
for everyone working in this repository, whichever tool they use, and it is
kept in one file on purpose — a second copy is a second thing to forget to
update.

Everything below is specific to working here with Claude Code.

## Before you write

- Open the document that owns what you are about to touch. `AGENTS.md` §2 has
  the map. The conventions are already decided; the cost of guessing is a
  reviewer discovering the guess.
- The data model is `AMS_Consolidated_Design_v2.sql`. Read the actual table
  before writing an entity configuration — the comments in that script explain
  *why* a column exists, which is usually the thing you need.

## Verifying

```bash
dotnet build AMS.slnx     # warnings and style violations are errors
dotnet test  AMS.slnx     # architecture rules run here
```

Both must be green before you report work as done. `dotnet build` failing on a
naming or analyzer rule is the system working, not an obstacle to route
around.

## Things that have already gone wrong here

- **Architecture tests that cannot fail.** The boundary rules read `.csproj`
  files, not compiled metadata, because `Assembly.GetReferencedAssemblies()`
  omits references nothing in the IL uses — the check passed while a module
  referenced another module. If you add a rule, prove it fails by breaking the
  thing it guards, then revert.
- **Analyzers disagreeing with `docs/`.** CA1000 and CA1716 both object to
  shapes doc 02 mandates. Resolved once in `.editorconfig` with the reason
  written down. Do the same; never scatter `#pragma warning disable`.
- **NuGet audit blocking restore.** Two transitive packages had known
  vulnerabilities. They are pinned in `Directory.Packages.props` under
  "Transitive pins". Do not disable the audit to get a build.

## Scope

Do what was asked. If you find something else wrong, say so — do not fix it
silently in the same change, and do not widen a schema or a standard without
flagging that it is a decision.
