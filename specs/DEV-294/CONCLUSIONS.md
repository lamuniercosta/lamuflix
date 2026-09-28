# DEV-294 grill conclusions

Append-only record of the `/grill-with-docs DEV-294` exchange (Keel ↔ Patron, 2026-09-26). The grill ran one round of 12 questions, which is the full cap. Each question was sent with Keel's recommendation. Patron's ruling and its cited source follow each one. Sources: recon-DEV-294 (Wisp), ticket *Scope & Technical Design* as quoted in recon §2, `specs/PRODUCT.md`, `docs/adr/0013-src-tests-solution-layout.md`, `CONTEXT.md`, `harness.yml`, `.cursor/rules/refactor-gate.mdc`, `Directory.Packages.props`.

Patron's source note: the architecture plan is not in this repo. Only ADR 0013:13/29 and `specs/DEV-290/spec.md:44` cite it, and no in-repo source names a Core test project. Patron treats that as a gap in the sources, not as permission to invent a project.

---

## Q1 - Test project for Core domain tests (§2.3 #2)

**Question.** `tests/LamuFlix.Test/LamuFlix.Test.csproj` does not reference `LamuFlix.Core`; only ArchitectureTests does (recon §6(2)). Where do the ticket's unit tests live?
**Keel recommended.** Add one ProjectReference to `src/LamuFlix.Core` in `LamuFlix.Test.csproj`, and put the tests in `tests/LamuFlix.Test/Domain/`. Do not create a new test project.
**Patron ruling: ACCEPT, not a checkbox.** Section 2.3 #2 covers a new project, top-level folder, or layer, and a reference to an existing in-solution project is none of those. Section 2.3 #1 covers NuGet/npm packages only. The ticket requires unit tests with a 100% branch-coverage AC (recon line 33), and those tests cannot exist without the reference. ADR 0013:29 says only that new projects go under `src/` or `tests/`. A NEW test project (for example `LamuFlix.Core.Tests`) is NOT authorized and would be `blocked: structural`. If the owner's off-repo plan §3 names a Core test project, the owner should raise it at Gate 1.
**Implication.** Exactly one `<ProjectReference>` line is added. This is an edit, not a §2.3 #6 rewrite.

---

## Q2 - Persistence of the aggregate tracking fields (§2.3 #3)

**Question.** Do `EnrichedAt`, `EnrichmentAttempts`, `LastFailureCategory`, and `LastAttemptAt` need a `movie`-table migration?
**Keel recommended.** No. Keep the domain in memory only for DEV-294, with no migration, no EF change, and no checkbox, and move persistence to a follow-up.
**Patron ruling: ACCEPT.** The ticket names these as fields on the aggregate root (recon line 30), not as columns, and it names no migration. `PRODUCT.md` §2 sequencing names no persistence work under DEV-282. Decision: file ONE follow-up ticket for persistence, rehydration, and adoption, with parent DEV-282, estimate 5, and tag `size:L`. Rigger records it.
**Implication.** No migration, no `LamuFlixContext` change, no rehydration factory.

---

## Q3 - Wiring into existing code (§2.3 #6)

**Question.** Should the Data EF entity, the enum, `EnrichmentJobProcessor`, or `MovieService` be edited to use the aggregate?
**Keel recommended.** No. The Core `EnrichmentStatus` stays independent of Data's `MovieEnrichmentStatus`.
**Patron ruling: ACCEPT.** Do not edit `src/LamuFlix.Data/Models/Movie.cs`, `src/LamuFlix.Data/Models/MovieEnrichmentStatus.cs`, `src/LamuFlix.Worker/Services/EnrichmentJobProcessor.cs` (lines 78, 84, 190), or `src/LamuFlix.Web/Services/MovieService.cs` (lines 336-352). The ticket names none of them (§2.3 #6; PRODUCT.md §5 item 6; recon lines 150-152). The duplicated enum values are accepted. Adoption rides the Q2 follow-up.

---

## Q4 - `MovieMetadata` type

**Question.** The ticket signature is `MarkEnriched(MovieMetadata metadata, DateTimeOffset now)`, but the existing `MovieMetadata` lives in Worker, which Core cannot reference.
**Keel recommended.** Add a new Core sealed record `LamuFlix.Core.Domain.MovieMetadata`.
**Patron ruling: ACCEPT.** The record has these fields: `Title` (string, non-blank), `Synopsis` (string?), `ReleaseYear?`, `Runtime?`, `ImdbRating?`, `ImdbId?`. It is built from the ticket's value objects (recon line 32) and is required by the ticket signature (recon line 29). Core must not reference Worker (recon line 46; ArchitectureTests.cs:24-48). The Worker `MovieMetadata` (recon line 67) is untouched. The aggregate property is `MovieMetadata? Metadata`.

---

## Q5 - Aggregate shape and creation

**Keel recommended / Patron ruling: ACCEPT.** `public sealed class Movie` in `LamuFlix.Core.Domain`, with private setters. Its properties are `Id` (MovieId), `Title` (string, non-blank), `Path` (LibraryPath), `Format` (MediaFormat), `IsInWatchlist` (bool), `Metadata` (MovieMetadata?), and the five ticket tracking fields (recon lines 28-31). It is created only through `static Movie.Create(MovieId, string title, LibraryPath, MediaFormat)`, which returns `Status = Pending`, `EnrichmentAttempts = 0`, not in the watchlist, and all nullables null. There is no rehydration factory; that rides Q2. Each type gets its own file, flat under `src/LamuFlix.Core/Domain/`, the directory the ticket names (recon line 24). `.gitkeep` stays in place.

---

## Q6 - Transition table

**Keel recommended / Patron ruling: ACCEPT as written.**
- `MarkEnriched`, `MarkNotFound`, and `MarkFailed` are legal only from `Pending`. From `Enriched`, `NotFound`, or `Failed` they throw `InvalidTransitionException`.
- Every legal `Mark*` call increments `EnrichmentAttempts` and sets `LastAttemptAt = now`.
  - `MarkEnriched` also sets `EnrichedAt = now` and `Metadata`, and clears `LastFailureCategory`.
  - `MarkFailed` also sets `LastFailureCategory`.
  - `MarkNotFound` changes only the common fields.
- `RequestEnrichment` is legal from `Enriched`, `NotFound`, and `Failed`, and moves to `Pending`. It throws from `Pending`. It keeps `EnrichmentAttempts`, which is monotonic and never reset. It also keeps `EnrichedAt`, `Metadata`, and `LastFailureCategory` as history.
- A call that throws mutates nothing.

**Basis.** The ticket requires that every illegal transition throws (recon line 33). This table is the Patron design ruling that fills the ticket's gap.

---

## Q7 - Watchlist semantics

**Keel recommended / Patron ruling: ACCEPT.** The watchlist is independent of `Status`. `AddToWatchlist` throws `InvalidTransitionException` when the movie is already in the watchlist, and `RemoveFromWatchlist` throws when it is absent. The existing `MovieService` methods are idempotent but are not wired here (Q3). The divergence is intentional: record it, and do not file it as a defect.

---

## Q8 - `InvalidTransitionException` and `EnrichmentFailureCategory`

**Keel recommended / Patron ruling: ACCEPT.** `public sealed class InvalidTransitionException : InvalidOperationException` lives in Core/Domain. It carries the attempted action name and the current state as a string, and has the standard message constructors. The ticket already fixes the HTTP 409 mapping (recon line 28). That mapping is recorded only in the XML doc, with no endpoint or middleware in DEV-294 (§2.3 #4/#6). The mapping itself rides a follow-up. `EnrichmentFailureCategory` members: `Unknown = 0, ProviderUnavailable = 1, RateLimited = 2, InvalidResponse = 3`. CONTEXT.md:31 defines Failure Category as "a caller-safe taxonomy" but names no members, so this list stands as a Patron design ruling.

---

## Q9 - Value-object rules

**Keel recommended / Patron ruling: ACCEPT, including ReleaseYear.**

**Uniform rule.** Each value object is a sealed record with an explicit validating constructor and a get-only `Value` property (no `init`), so a `with` expression cannot bypass validation. Violations throw the `ArgumentException` family.

| Type | Rule |
|---|---|
| `MovieId` | Value > 0 |
| `ImdbId` | Non-null and matches `^tt\d{7,8}$`, checked with `[GeneratedRegex]` (BCL, no package, so §2.3 #1 does not apply) |
| `ImdbRating` | 0.0-10.0 inclusive. More than one decimal place is rejected, not rounded. |
| `Runtime` | `Minutes` > 0 |
| `LibraryPath` | Non-blank. Any literal `..` substring is rejected, as the ticket's text says (no segment parsing). |
| `MediaFormat` | Non-blank. `Extension` is trimmed, its leading `.` is stripped, and it is lowercased. |
| `ReleaseYear` | Constructed as `ReleaseYear(int value, DateTimeOffset now)` and requires `1888 <= value <= now.Year + 5`. Equality uses `Value` only. |

**ReleaseYear rationale.** The ticket's "current+5" bound needs a clock. `DateTime.Now`/`UtcNow` are banned (BannedSymbols.txt; recon line 118), and the ticket's own methods already take `DateTimeOffset now` (recon line 29). The extra constructor parameter is therefore consistent with the ticket's intent and is not a re-decision. Record it as a Patron ruling, not a checkbox.
**Recon correction.** CPM pins Shouldly **4.3.0** (`Directory.Packages.props:21`), not 4.2.3 as recon §5 line 110 says.

---

## Q10 - Test strategy, coverage evidence, property tests (SPLIT)

**Keel recommended.**
- Use xUnit v3 and Shouldly.
- Cover the state machine with an exhaustive Theory matrix.
- Evidence the branch-coverage AC with that matrix plus Stryker.
- Record a property-test opt-out, since FsCheck is not in CPM.

**(a) Patron: ACCEPT.**
- Use xUnit v3 and Shouldly from the existing CPM (`Directory.Packages.props:18,21`).
- The state-machine Theory matrix covers 4 states × 4 enrichment actions, plus 2 watchlist states × 2 actions. Each case asserts either the legal result or the throw, and asserts that nothing mutates on a throw.
- Value-object boundary tests cover each Q9 rule.
- The 100% branch-coverage AC is evidenced by the matrix plus Stryker (`gates.mutation.threshold` 80, never lowered).
- No coverage collector is referenced (`tests/LamuFlix.Test/LamuFlix.Test.csproj:11-16`), so no branch report is attached. Adding a coverage package would be §2.3 #1 and is NOT authorized.

**(b) Patron: `blocked: structural`.** Should FsCheck (a new NuGet dependency, §2.3 #1) be added to `Directory.Packages.props` so the refactor gate's property tests can cover DEV-294's domain logic?
- `.cursor/rules/refactor-gate.mdc` and `.agents/skills/refactor/SKILL.md:22` make property tests mandatory for pure/domain logic.
- `harness.yml` (`gates.propertyTests`, lines 31-35) allows the per-ticket opt-out only for "a ticket with no domain invariants", and this ticket has domain invariants.
- A recorded opt-out would therefore contradict the gate's own precondition.

**Owner checkbox.**
- YES: authorize FsCheck and the property tests.
- NO: the team must re-decide how the refactor property-tests gate is satisfied.

Until the owner answers, Gate 1 stays closed and `run-property-tests` is reported SKIPPED/OPT-OUT, never PASS.
**Keel verification.** Keel read `harness.yml:31-35` and confirmed the precondition text.

---

## Q11 - Loop terms

**Keel recommended.** Critical, High, and Medium block. Frozen scope as stated. Two rounds.
**Patron ruling: ACCEPT with the severity bar corrected to repo precedent.** Every verified in-scope Critical or High finding is fixed and re-verified before the PR. Medium and Low findings are recorded as follow-ups with their severity intact (`specs/DEV-291/brief.md:19`; Keel verified the line). Medium does not hard-block this round.
- Frozen scope: the Core/Domain types above, the single ProjectReference, and the tests in `tests/LamuFlix.Test/Domain`. Anything else is a follow-up issue that Rigger records, not a finding in this round.
- Round cap: 2 formal review rounds, with at most 2 fix commits per round. A third round escalates to the user.
- `/ship-review` runs because the ticket is size L.

---

## Q12 - ADR and glossary

**Keel recommended / Patron ruling: ACCEPT with two corrections.** Keel drafts `docs/adr/0014-core-movie-aggregate-separate-from-ef-entity.md`. 0014 is the correct number, since the highest existing ADR is 0013. Two corrections:
- `CONTEXT.md:27-29` (Enrichment Status) and `CONTEXT.md:38` (Watchlist) already exist. EXTEND them, do not duplicate them.
- Add only the new terms "Movie Aggregate" and "Enrichment Attempt".

An ADR is documentation, so it is not a structural item.

---

## Grill outcome

The 12/12 cap is reached. One owner checkbox remains: Q10(b), FsCheck. Everything else was accepted or ruled by Patron with a cited source. No taste assumptions were made, so no `ASSUMPTIONS.md` is written. The resolved Q&A above is now safe to compact.

---

## Conductor steer on Q12 and on write scope (Bernstein, 2026-09-26)

**Context.** After the grill, Keel reported every ruling to the Conductor before writing anything. This entry does not amend Patron's Q12 ruling. It records how Q12 is carried out.

**Steer.**
1. **CONTEXT.md.** The ticket does not name the existing `CONTEXT.md`, so the glossary extension (extending Enrichment Status and Watchlist, and adding Movie Aggregate and Enrichment Attempt) becomes an unticked owner checkbox under §2.3 #6. Nobody edits `CONTEXT.md` until the owner answers.
2. **ADR 0014.** The ADR is still required for a size-L ticket (task-pipeline Phase 2 step 5). Keel drafts it in a later Phase A ADR step, after the brief, not in the grill write.
3. **Write scope for this step.** Only this file, `specs/DEV-294/brief.md`, and the DEV-294 task note. No source files, and no `spec.md`, `plan.md` or `tasks.md`.

**Implication.** There are two owner checkboxes: Q10(b) FsCheck (§2.3 #1) and the CONTEXT.md glossary edit (§2.3 #6). Gate 1 stays closed until the owner answers both.

---

## Owner answers (user, 2026-09-26)

- **Q10(b) FsCheck: YES.** FsCheck is authorized in `Directory.Packages.props`. Property tests for the value-object invariants and the transition table are frozen Phase B scope, following the "If Q10(b) = YES" branch in `tasks.md`. `run-property-tests` must PASS; the SKIPPED/OPT-OUT fallback no longer applies.
- **CONTEXT.md glossary: YES.** Phase B extends **Enrichment Status** (`CONTEXT.md:27-29`) and **Watchlist** (`CONTEXT.md:38`) and adds **Movie Aggregate** and **Enrichment Attempt**, as in Patron's Q12 ruling. This satisfies constitution VIII, so D5 no longer applies.

**Owner policy note.** The owner said neither question should have been escalated. Under the revised §2.3 (`specs/PRODUCT.md` §5, role files updated the same day), Patron rules on dependencies, new files and unnamed-file edits with a cited basis. Only two things go to the owner: a change that contradicts or would change the ticket text, and a constitution departure Patron judges necessary.

## D7 Architect invocation ruling (Patron, 2026-09-27)

**Decision: AMEND D7, not the ticket.** The frozen `dotnet stryker` worktree invocation cannot measure DEV-294: dotnet-stryker 4.16.0 resolves the linked worktree gitdir to the main checkout, then compares main's workdir and prefixes diff paths with the main root; none can match worktree mutant paths (`recon-DEV-294:82-97`, `DEV-294:1166-1179`). Keep Architect FAIL until a scored replacement run passes.

**Exact amended route for Keel to freeze in brief.md before any run:** From `F:\Dev\LamuFlix.worktrees\DEV-294` on `feature/DEV-294`, create one JSON config under `$env:TEMP` by copying the current `stryker-config.json` settings, replacing `since` with `{ "enabled": false }`, and adding `mutate` with exactly these Core-project-relative globs: `Domain/EnrichmentStatus.cs`, `Domain/ImdbId.cs`, `Domain/ImdbRating.cs`, `Domain/InvalidTransitionException.cs`, `Domain/LibraryPath.cs`, `Domain/MediaFormat.cs`, `Domain/Movie.cs`, `Domain/MovieId.cs`, `Domain/MovieMetadata.cs`, `Domain/ReleaseYear.cs`, `Domain/Runtime.cs`. Preserve `mutation-level`, `reporters`, and all three existing thresholds without hardcoding or lowering them. Invoke `dotnet stryker -f <absolute OS-temp config path> -p LamuFlix.Core.csproj` from that worktree root. Do not edit tracked `stryker-config.json` or `harness.yml`, run from or change the main checkout, move refs, or rebase. Capture config path/hash and report; require native exit 0, at least one tested mutant, mutated files only among those eleven paths with Core Domain covered, and score >=80%. Zero tested mutants, no score, any other mutated path, or an unrunnable invocation remains FAIL and returns to Keel; no retry by changing scope without a new ruling.

**Basis and escalation:** D7's purpose and stop bar are only changed Core Domain files and >=80% (`brief.md:178-181`; `spec.md:19,64-67`; `tasks.md:191,221`). The constitution requires Stryker at break threshold 80 on Core (`.specify/memory/constitution.md:259-265`) and tested legal/illegal Movie transitions (`:330-331`); the amended run preserves those requirements. `specs/PRODUCT.md:34-48` §2.3 #5 does not apply because this touches no LocalPlay flag, secret, or Process.Start; #6 is respected because the config is a new disposable OS-temp file and no unnamed existing repository file is deleted or rewritten. Patron's `DEV-294:1022-1026` forbids main-checkout actions; this route stays in the named worktree. The ticket deliverables and acceptance criteria do not change, and no constitution departure is sought, so §2.3(a)/(b) does not apply and there is no owner checkbox. Keel records this amendment in brief.md before any run; this ruling alone is not an Architect PASS.

## Phase 4 review follow-up grouping (Patron, 2026-09-27)

Keel verified each row on HEAD `0a321517` at DEV-294:1249-1274. The closing bar is Critical/High only (`brief.md:133-139`), so these Medium/Low items are follow-ups, not in-round remediation. Parent epic for new tickets is DEV-282 (`brief.md:7,141-144`). Rigger alone records YouTrack changes (`lamuflix-team-charter:28`) and reads them back. Each ticket description must retain the labels, original severities, and verdicts below; grouping by root cause does not merge the finding rows.

| Record | Title / disposition | Source rows to preserve | Parent, estimate, size | Scope |
|---|---|---|---|---|
| New A | Restrict IMDb ID digits to ASCII | #1 Sentry P1, Medium, CONFIRMED; #8 Compass F2, Low, CONFIRMED | DEV-282; 1d; `size:S` | Amend `spec.md`/`brief.md` from `\d` to `[0-9]`; update `ImdbId` regex and independent property oracle; add non-ASCII digit regression. One root cause, both reviewer records. Cite DEV-294:1255,1262. |
| New B | Set one canonical MediaFormat extension rule | #2 Sentry P2, Medium, CONFIRMED; #5 Ledger S1, Low, PLAUSIBLE | DEV-282; 1d; `size:S` | Revisit frozen D3 for `..mkv` (choose strip-all or reject), then align spec, implementation, boundary examples, and an independent property oracle. Do not silently alter D3 in DEV-294. Cite DEV-294:1256,1259. |
| Existing follow-up #1 | Persistence, rehydration, and adoption of aggregate | #3 Sentry P3, Low, PLAUSIBLE | DEV-282; existing estimate 5; existing `size:L` | Append the LibraryPath substring/root-path concern and its concrete `Movies..2025` scenario to the existing brief.md:143 follow-up. Preserve the original persistence scope; do not create a second LibraryPath ticket. Cite DEV-294:1257 and findings-DEV-294-Sentry:91-94. |
| New C | Remove duplicated enrichment-status fixture switches | #6 Ledger S2, Low, PLAUSIBLE | DEV-282; 2h; `size:S` | Make the MovieTests/PropertyTests status setup consistent through one test helper or independent fixture strategy; retain behavioral coverage. Cite DEV-294:1260 and findings-DEV-294-Ledger:42-45. |
| New D | Remove duplicate Core ProjectReference from LamuFlix.Test | #7 Compass F1, Medium, CONFIRMED | DEV-282; 1h; `size:S` | Delete the added csproj line 22; keep the pre-existing line 27; verify one reference and build. Cite DEV-294:1261 and findings-DEV-294-Compass:93-97. |
| New E | Correct DEV-294 failure-category spec drift | #9 Compass F3, Low, CONFIRMED | DEV-282; 2h; `size:S` | Correct `spec.md`, `brief.md`, and `tasks.md` to describe the DEV-295 sealed-record type already in Core; no code change. Cite DEV-294:1263 and findings-DEV-294-Compass:105-109. |
| DEV-294 note only | Commit-order process deviation | #10 Compass F4, Low, CONFIRMED | No ticket | The glossary landed in commit 1 rather than frozen D11 Commit 6; final `CONTEXT.md` content is correct. Record here and in DEV-294 only; no YouTrack comment or follow-up ticket. Cite DEV-294:1264 and findings-DEV-294-Compass:111-115. |

Sentry P4 is no action under D9, outside the findings JSON (`DEV-294:1258`); do not file it. The five new ticket estimates and `size:S` tags are Patron planning judgments for bounded follow-up work; they do not change DEV-294 deliverables, the chain, or the cleared review verdict (`DEV-294:1268-1270`). Rigger should check for an already filed matching ticket before creating each new one, use temporary description files, report each ID and read-back `(verified)`, and never use comment `7-219` as review evidence. Removal of that comment remains a separate blocked route (`DEV-294:1242-1247,1273`).

## Stopped Phase 4 handoff ruling (Patron, 2026-09-27)

DEV-377 and DEV-378 stand as the authorized A/B children; no correction or withdrawal. Keep finding #3 appended to the existing C follow-up at `brief.md:143`, without a separate LibraryPath ticket (`DEV-294:1278-1280,1288-1289`; rows above).

Preserve both unstaged YouTrack script diffs and all five scratch description files in place. No checkout, reset, edit, delete, or cleanup is authorized. Their ownership, provenance, and any repair belong to a separate bounded maintenance investigation after Phase 4, with no automatic ticket or DEV-294 scope expansion. Current diffs and file hashes are established, but pre-checkout script content and scratch Git classification are not (`DEV-294:1287-1289`; `recon-DEV-294:111-112`).

Rigger may resume only the three absent DEV-282 children: C status-fixture duplication (2h, `size:S`), D duplicate Core ProjectReference (1h, `size:S`), and E failure-category spec drift (2h, `size:S`). Before each creation, check the live tracker for a matching ticket and use a description matching the exact frozen row above. Use the existing YouTrack tool as-is; read back each ID, parent, estimate, size tag, and description, then report `(verified)`. Stop on the first mismatch or tool failure. No script edits, tests, gates, cleanup, other worktree edits, other YouTrack action, comment `7-219` action, PR, or active-chain change (`DEV-294:1281-1285,1287-1289`; `recon-DEV-294:111-112`).
