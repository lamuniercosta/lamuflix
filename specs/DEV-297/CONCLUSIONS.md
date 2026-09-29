# DEV-297 conclusions

## Phase 4 review decisions at 60409113 (P-1/P-2)

**Patron ruling:** Confirm nullable `MovieQuery.Sort` and `Direction` as the intended Core API shape. They may be absent on construction so the validator can reject them; a successfully parsed or formatted query has both values. K-F3 is a spec-text correction at `spec.md:104,143`, not a source change. Confirm the existing Web-to-Core `ProjectReference` needed by the FR-011 compatibility implementation. K-F4 is a plan-text correction at `plan.md:153`. Keel may record these in `brief.md` and route the text corrections; neither decision opens a user checkbox.

- `specs/DEV-297/spec.md:109,112,120-124,143-147` requires the codec to reject missing sort/direction, the validator to require them, and Web's closed legacy compatibility path. `recon-DEV-297:402-415` locates the implemented nullable fields and project reference and identifies the two text mismatches.
- `specs/DEV-297/plan.md:150-154` calls Web unchanged despite its required Core reference. `specs/PRODUCT.md:32-46` assigns API-shape care rulings to Patron; this ruling changes no ticket deliverable or constitution rule.

**Patron ruling:** Record K-F1 on DEV-298 as the wiring consumer's latent leading-`?` parser risk; Rigger should add the cited comment. For K-F2, Rigger should check open tickets for the empty `EntityExtensions` class and orphaned `ViewModelExtensions` helpers, fold it into a suitable existing cleanup ticket if one exists, or file one follow-up dead-code ticket. No DEV-297 source change is authorized by this disposition. K-F5 (sub-threshold duplication) and K-F6 (helper naming) need no action: noted, no ticket.

- `recon-DEV-297:396-416` confirms K-F1 at `MovieQueryString.cs:86` is latent until DEV-298/299 consumption, K-F2 at `EntityExtensions.cs:3-5` and `ViewModelExtensions.cs:7-58`, and K-F5/K-F6 as Low maintenance findings. `specs/DEV-297/brief.md:191-198` sends Low review findings to follow-up; `specs/PRODUCT.md:18,32-48` avoids a new abstraction and reserves tracked unnamed-file deletion for a cited Patron ruling.

---

## Final Spec Kit analysis disposition (round 3 of 3)

**Patron ruling:** DEV-297 remains blocked at the three-round cap. R3-1 through R3-6 and the listed Low findings are defects in the current spec, plan, and tasks against already decided scope; they raise no new ticket-change or constitution-departure question. The existing combined Q4/Q5/Q11 owner checkbox remains unanswered. No YouTrack change or follow-up ticket is needed; Low findings are noted, no ticket. Corrections require a separately authorized future phase. No fourth analysis round, plan challenge, freeze, or Gate 1 approval follows from this disposition.

- DEV-297:12-35 fixes the typed fields, page size, validator, and FsCheck round trip; `specs/DEV-297/brief.md`:19,90-91 fixes JSON-by-Name and the required `pageSize` key. R3-1, R3-2, and R3-6 are omissions or contradictions of those decisions.
- DEV-297 note:121-132 records the six blockers, Low findings, and final-round cap; `specs/DEV-297/brief.md`:191-196 requires `blocked` after three rounds and blocks in-scope Medium spec defects. R3-3 through R3-5 and the Low findings remain artifact corrections within DEV-297.
- `specs/PRODUCT.md`:23-24,32-39 reserves owner escalation for a ticket change or necessary constitution departure; `specs/DEV-297/CONCLUSIONS.md` Q4/Q5/Q11 already carries the combined owner checkbox. The findings add no new owner decision or separate tracker work.

## Web-gate membership for DEV-297

**Patron ruling:** `./scripts/run-web-gates.ps1` is outside DEV-297's required gate set because its frozen delivery changes no file under `web/`. Do not schedule it for this ticket; report `N/A: no web/ diff`, never PASS or a required scope-empty SKIPPED. This ruling addresses membership only. The existing rule that an exit 2 scope-empty result from any required gate blocks remains in force.

- DEV-297 frozen scope in `specs/DEV-297/brief.md`:145-159 and recon-DEV-297 R2:61-86 identify no `web/` edit; `specs/DEV-297/spec.md`:159-162 excludes Web API contract changes.
- `task-pipeline`:83 invokes `scripts/run-web-gates.ps1` for `/web` work under web-implement; line 94 identifies exit 2 when `/web` has not changed. The Patron role charter states that `/web` builds with web-implement.
- `specs/DEV-297/brief.md`:174-183 lists the applicable .NET gates and records the proposed `N/A: no web/ diff` handling; this ruling ratifies membership only.

---

## Q1 dependency versions and consumers

**Patron ruling:** Pin `Ardalis.SmartEnum` at exact CPM version `8.2.0` and `Ardalis.SmartEnum.SystemTextJson` at exact CPM version `8.1.0`. These are the latest stable releases shown by NuGet for each package as of 2026-09-29; both are compatible with net10.0, and the JSON package accepts SmartEnum >= 8.1.0. The packages share major version 8, but their published patch/minor versions differ; T002 must not require identical numbers. Require `./scripts/run-vulnerable-packages.ps1` exit 0 and record both chosen versions in the PR body; this is a delivery check, not a gate result asserted here.

**Patron ruling:** Reference `Ardalis.SmartEnum` from `LamuFlix.Core`. Reference `Ardalis.SmartEnum.SystemTextJson` only from `tests/LamuFlix.UnitTests` for JSON-by-Name round-trip tests of `EnrichmentStatus`, `MovieSort`, and `SortDirection`; no production project needs its converter in DEV-297.

- DEV-297:12-22 and 30-35 fix the closed sets and test project; `specs/DEV-297/spec.md` FR-009 requires CPM entries and references only where conversion is needed. `specs/PRODUCT.md` §5 assigns the dependency decision to Patron.
- NuGet package pages: https://www.nuget.org/packages/Ardalis.SmartEnum/8.2.0 and https://www.nuget.org/packages/Ardalis.SmartEnum.SystemTextJson/8.1.0 show the exact versions and net10.0 compatibility; the JSON package declares `Ardalis.SmartEnum >= 8.1.0`.
- recon-DEV-297:155-161 finds zero production serializers of Core `EnrichmentStatus`; `specs/DEV-297/brief.md`:17-21 identifies Core and UnitTests as the relevant consumers.

---

## Q1 — MovieSort and SortDirection representation

Keel asked whether the ticket's `enum` wording requires C# enums or permits the constitution's Enumeration types. Keel recommended sealed SmartEnum types with exactly the ticket's MovieSort values (`Title`, `Year`, `Rating`, `Runtime`) and SortDirection values (`Ascending`, `Descending`), parsed by name and serialized by `Name`. Keel noted that query execution and nullable-sort behavior belong to later wiring, outside this ticket, and requested a package-presence check before settling dependency handling.

**Patron ruling:** Use sealed Enumeration types for `MovieSort` and `SortDirection`, preserving the ticket's exact closed sets. Here, `enum` names the set of values, while the constitution determines the C# representation. This changes neither the ticket's scope nor its acceptance criteria. Package presence and any required dependency ruling remain a separate check.

- DEV-297:12-22 fixes the fields and allowed values; DEV-297:33-36 fixes the acceptance criteria.
- Constitution 1.2.0 §III, lines 147-163, requires `MovieSort` and `SortDirection` Enumerations and a closed sort whitelist; Coding Conventions, lines 408-410, forbids project-owned C# enums.
- `specs/PRODUCT.md` §3 makes ticket scope authoritative; §5 requires a cited Patron ruling for a dependency change.

---

## Q1 addendum — Enumeration packages

Bernstein relayed recon-DEV-297:58-64: the central package file and project references have no Ardalis.SmartEnum packages. The accepted Enumeration design therefore requires a new NuGet dependency.

**Patron ruling:** Add `Ardalis.SmartEnum` for the Core Enumeration types and `Ardalis.SmartEnum.SystemTextJson` where their name-based JSON conversion is needed. This is an explicit care-list dependency ruling, not an owner checkbox. Keep the values and public names fixed by DEV-297.

- Constitution 1.2.0 §I, lines 112-117, permits Ardalis.SmartEnum in Core; Coding Conventions, lines 408-410, requires Enumerations for closed sets.
- Constitution 1.2.0 Technology Stack Constraints, lines 329-330, selects `Ardalis.SmartEnum` and its System.Text.Json name converters.
- `specs/PRODUCT.md` §5, care item 1, assigns a deliberate cited dependency ruling to Patron; DEV-297:12-22 specifies the delivered values.

---

## Test project named by the ticket

Bernstein relayed recon-DEV-297:58-64: `tests/LamuFlix.UnitTests` does not exist, while `tests/LamuFlix.Test` already has FsCheck.Xunit.v3.

**Patron ruling:** The FsCheck round-trip property belongs in `LamuFlix.UnitTests`, the project expressly named by DEV-297. Creating that named project is already within ticket scope. The existing `LamuFlix.Test` package does not silently substitute a different project for the named deliverable.

- DEV-297:30-35 names `LamuFlix.UnitTests` and requires the FsCheck property; `specs/PRODUCT.md` §3 makes that ticket text authoritative.
- Constitution 1.2.0 Testing Matrix, lines 285-288, locates property tests in `LamuFlix.UnitTests` with FsCheck.Xunit.
- `specs/PRODUCT.md` §5, care item 2, requires a ruling for a new project only when it is not already decided in the ticket.

---

## Q2 — Replacing the legacy reflection query strings

Keel asked whether DEV-297's acceptance criterion requires deleting `DynamicQuery` and `DynamicSort` from `EntityExtensions.cs` and updating `MovieService`, which currently calls them. Keel recommended documenting them as superseded and deferring removal to a browse-handler ticket, while noting that this might narrow the literal criterion. The DEV-282 child ownership addendum found that DEV-299 owns a future `BrowseMoviesQueryHandler` and DEV-298 its port, but no child owns the existing IQueryable filter/sort predicates or legacy helper removal.

**Patron ruling:** DEV-297 must replace and remove the reflection-based `DynamicQuery` and `DynamicSort` helpers in this PR and update their live `MovieService` callers to use the typed model and explicit sort whitelist while preserving the current browse behavior and external contract. The overview and acceptance criterion force these edits. They are within ticket scope, so no owner checkbox is needed. The earlier message that this ticket has no query execution is superseded by this ruling; DEV-299 still owns its separate future handler work.

- DEV-297:9-10 and 33-36 expressly require replacing the legacy reflection query strings in `EntityExtensions.cs`; recon-DEV-297:37-46 identifies their live `MovieService` callers.
- recon-DEV-297:66-86 assigns future browse handler/port work to DEV-299/DEV-298 and finds no sibling assignment for legacy helper removal.
- Constitution 1.2.0 §III, lines 147-163, bans reflection queries and requires typed predicates and a closed sort whitelist; `specs/PRODUCT.md` §§3, 5 makes the ticket authoritative and treats acceptance-forced edits as in scope.

---

## Q3 — Core EnrichmentStatus used by MovieQuery

Keel asked whether the existing plain C# `EnrichmentStatus` may be used in the new `MovieQuery.Statuses` field, converted in this ticket, or converted in a separate dependency ticket. Keel recommended conversion in this ticket if recon showed a bounded ripple. recon-DEV-297:88-94 found one Core production consumer (`Movie.cs`), three existing test consumers, and no Infrastructure, Api, EF mapping, or migration references; the legacy Data `MovieEnrichmentStatus` is separate.

**Patron ruling:** Convert `Core/Domain/EnrichmentStatus.cs` to `SmartEnum<EnrichmentStatus, int>` in DEV-297, preserving `Pending=0`, `Enriched=1`, `NotFound=2`, and `Failed=3`. Adapt `Movie.cs` and its three existing test consumers as necessary without changing behavior. This is a deliberate care-list file-scope ruling required by the ticket's new Core signature and the constitution; no database schema change is authorized. Leave the separate Data `MovieEnrichmentStatus` untouched.

- DEV-297:18 names `ImmutableArray<EnrichmentStatus> Statuses`; constitution 1.2.0 §IV requires the four numeric SmartEnum values, and Coding Conventions, lines 408-410, forbids plain C# enums in new Core signatures.
- recon-DEV-297:88-94 bounds the consumers and confirms no EF conversion or migration; constitution 1.2.0 sync report, lines 4-17, specifies the Enumeration representation and preserves the numeric database contract.
- `specs/PRODUCT.md` §5, care items 3 and 6, assigns this cited file-scope decision to Patron; this ruling does not change ticket deliverables or the constitution.

---

## Q4 — Homes for query types, validator, and predicates

Keel asked where the new Core query types, FluentValidation validator, and typed predicates belong. Keel recommended `Core/Library` for the query types, `Infrastructure/Library` for the validator, and an explicit compatibility implementation in transitional `LamuFlix.Web` for the live callers over `LamuFlix.Data.Models.Movie`. recon-DEV-297:100-179 confirms the Web service directly queries that legacy entity; Infrastructure cannot reference it under constitution §I, and the MVC path bypasses the handler validation decorator.

**Patron ruling:** Put `MovieQuery`, its range/page value objects, `MovieSort`, and `SortDirection` in `Core/Library`; put `MovieQueryValidator` in `Infrastructure/Library` for the canonical handler path. The proposed Web predicate home is a necessary constitution §III departure if DEV-297 must replace the live legacy helpers now: §III assigns query predicates to Infrastructure, while §I prevents Infrastructure from seeing the legacy Data entity. Patron cannot waive that departure. Keep the Web compatibility design and any public behavior changes open pending the owner's checkbox; do not treat this as Gate 1 approval.

- DEV-297:12-28 fixes the query shape and validator; constitution 1.2.0 §I, lines 112-122, limits Core/Infrastructure dependencies, and §III, lines 147-163, places query predicates in Infrastructure.
- recon-DEV-297:100-179 shows the live MVC/Web path uses `Data.Models.Movie` directly and bypasses handlers; constitution Known Technical Debt requires new transitional code to follow Principles II–IX.
- `specs/PRODUCT.md` §5 sends a constitution departure to the owner as `blocked: structural`; the ticket's replacement acceptance criterion remains DEV-297:33-36.

- [ ] **Owner decision for spec PR:** May DEV-297 temporarily place explicit, non-reflection filter/sort predicates for the live legacy `Data.Models.Movie` path in `LamuFlix.Web`, departing from constitution §III's Infrastructure placement until DEV-298/DEV-299 migrate browse? If not, which ticket or constitution change should own the full migration needed to meet DEV-297:34?

---

## Q2 qualification after legacy-path recon

recon-DEV-297:181-189 shows current Razor sorting includes `MetaScore`, `RottenTomatoes`, and `Id`, outside DEV-297's `MovieSort` values; current filtering also includes `DirectorId` and `CollectionId`, outside `MovieQuery`. The Q2 ruling still requires replacing the reflection helpers in DEV-297, but its proposed preservation of every existing legacy behavior cannot be assumed to follow from the canonical `MovieQuery` whitelist. Keel must reconcile the compatibility behavior in the grill, and any ticket-scope or constitution change goes to the owner checkbox.

---

## Q5 — Legacy behavior and canonical MovieQuery

Keel presented three options: a separate typed legacy compatibility model in Web; forcing the old UI through the narrower `MovieQuery` and dropping sorts/filters; or expanding the ticket's fixed canonical fields. Keel recommended the compatibility model with explicit predicates, a closed legacy sort set, and a safe fallback for unknown sort names. Keel also recommended that the canonical `MovieQuery` first be consumed by DEV-299.

**Patron ruling:** The legacy compatibility model is the preferred conditional design: preserve the existing Razor sort and filter choices with explicit, non-reflection expressions, including a closed legacy sort set. Invalid unknown sort names may fall back to the existing `Id` descending default instead of throwing. Do not add legacy-only fields to `MovieQuery` or drop current UI choices. This design requires an owner decision: DEV-297's overview says the reflection helpers are replaced *with* the typed `MovieQuery` model, while the proposed bridge does not consume `MovieQuery`, and Q4 identified a constitution §III placement departure. It is not accepted as unqualified ticket completion until the checkbox is answered.

- DEV-297:9-22 fixes the canonical model and whitelist; recon-DEV-297:175-189 shows the wider legacy sort/filter surface and the current unknown-sort exception.
- Constitution 1.2.0 §III, lines 147-163, requires a closed typed sort set and forbids reflection; `specs/PRODUCT.md` §5 reserves ticket changes and constitution departures for owner checkboxes.
- recon-DEV-297:66-86 assigns the future browse handler to DEV-299, but DEV-297:34 assigns legacy reflection replacement to this ticket.

- [ ] **Owner decision for spec PR, combined with Q4:** May DEV-297 replace the live legacy reflection path with an explicit typed Web compatibility model that preserves its wider UI fields, while canonical `MovieQuery` is first consumed by DEV-299? This is a temporary constitution §III predicate-placement departure and a clarification of DEV-297:9-10's replacement wording.

---

## Q6 — Legacy null ordering and page size

Keel asked whether the new legacy compatibility sort must put null values last and whether the legacy `QueryParams.PageSize` must adopt `MovieQuery`'s 1–100 limit. Keel recommended explicit nulls-last ordering for nullable keys in both directions and leaving the legacy page-size and fixed-page request behavior intact.

**Patron ruling:** Use explicit `NULLS LAST` for nullable keys in the conditional compatibility sort and in the canonical `MovieSort` contract. Keep the existing Razor `QueryParams` binding and fixed `Page=1` behavior; apply the 1–100 size and positive-number rules to `MovieQueryValidator`, the type DEV-297 names. Do not silently clamp legacy page size or change the MVC response contract as part of this ticket. Any new internal compatibility query must still receive validation appropriate to its own contract under constitution §V; it does not inherit `MovieQuery`'s 100-size rule merely by proximity.

- Constitution 1.2.0 §III, lines 154-163, mandates nulls-last behavior and the `MovieQuery` validator; §V, lines 196-199, requires validation of new query contracts.
- DEV-297:23-27 names validation rules for `MovieQuery`; recon-DEV-297:175-189 shows the existing legacy page and sort behavior.
- `specs/PRODUCT.md` §3 preserves the ticket's fixed scope; the conditional Web path remains under the Q4/Q5 owner checkbox.

---

## Q7 — Query-string codec and round-trip property

Keel recommended a Core `MovieQueryString` codec with repeated ordered array keys, name-based Enumeration values, omitted nulls, and always-emitted sort/direction/page values. Keel proposed sequence-based equality for `ImmutableArray` members and an FsCheck generator restricted to validator-accepted queries. Keel proposed excluding empty `Text` values from the valid domain.

**Patron ruling:** Add the BCL-only Core codec as the required enabler of the ticket's round-trip property. Preserve every validator-accepted `MovieQuery`, including an empty `Text` unless the ticket's validator expressly rejects it; encode empty text distinctly from null. Use sequence equality for the three immutable arrays, preserving element order, and make the property assert parse success plus equality after format/parse. Include URL-hostile characters and prove the generator's values pass `MovieQueryValidator`. The proposed query-key spelling is a taste assumption, recorded separately. Do not narrow the valid domain merely to make the property pass.

- DEV-297:23-31 and 35 require validation and an FsCheck round trip for any valid `MovieQuery`; constitution 1.2.0 §III, lines 160-163, makes URL state authoritative.
- Constitution 1.2.0 §I permits BCL-only Core code; Testing Matrix, lines 285-288, puts FsCheck properties in `LamuFlix.UnitTests`.
- `specs/PRODUCT.md` §4 requires taste assumptions in `ASSUMPTIONS.md`.

---

## D1 — Empty ranges in the Q7 codec

**Patron ruling:** Choose B. When `Runtime` is non-null, `MovieQueryString.Format` emits `runtimeMin`, `runtimeMax`, and `runtimeIncludeUnknown`, writing an empty value for each null bound. When `Year` is non-null, it emits `yearMin` and `yearMax`, likewise writing an empty value for each null bound. Absent range keys mean a null range; present empty bounds mean null bounds in a present range. Thus `RuntimeRange(null, null, false)` and `YearRange(null, null)` remain distinct from null ranges and round-trip unchanged. A partially present range is malformed and `TryParse` fails under the failure policy Keel records for M9. Other null values remain governed by Q7. Do not add a validator rejection for empty ranges.

- DEV-297:12-27 defines nullable range objects and validation only for page and `Min <= Max`; lines 30-31 and 35 require the FsCheck round trip for every valid `MovieQuery`.
- Constitution 1.2.0 §III, lines 160-163, makes the query string authoritative and requires an unchanged round trip; Q7 in this file preserves the full validator-accepted domain.
- `specs/DEV-297/brief.md`:60-76 sets the BCL-only codec and Q7 key names; the presence encoding refines Q7's omitted-null rule for range objects without changing ticket scope.

---

## Q8 — Tests for the compatibility path and status conversion

Keel proposed pure IQueryable characterization tests for the legacy expressions, a real-database translation smoke test using the existing PostgreSQL Testcontainers fixture, and status-value/name tests after converting Core `EnrichmentStatus`. recon-DEV-297:194-199 confirmed the production legacy provider is MySQL, while existing test infrastructure uses PostgreSQL and `LamuFlix.Test` can access Web internals. Keel recommended recording the untested MySQL-specific translation risk instead of adding a new MySQL test dependency.

**Patron ruling:** Put conditional Web compatibility characterization tests in the existing `LamuFlix.Test` project, including each sort and predicate, both directions, and null placement. Use the existing real PostgreSQL fixture for an EF translation smoke test; this proves relational translation on that provider, not production MySQL equivalence. Record the remaining provider risk in Keel's brief and ADR-0009. Add Core status value/name tests in `LamuFlix.UnitTests`, keep the existing `Movie` assertions intact, and require the normal full test and applicable Core mutation gates at implementation verification. Do not add a MySQL Testcontainers dependency for this transitional path.

- recon-DEV-297:88-94, 191-199 bounds the affected status consumers, missing legacy tests, available PostgreSQL fixture, and production provider difference.
- Constitution 1.2.0 Principle IX and Technology Stack Constraints require real persistence testing and phase-appropriate Core mutation coverage; DEV-297:30-35 requires the FsCheck property.
- `specs/PRODUCT.md` §5 care items 1 and 6 assigns the dependency/file-scope decision to Patron; the Q4/Q5 compatibility design remains conditional on the owner checkbox.

---

## Q9 — ADR-0009 content and status

Keel asked for the filename, status, and content of the ticket's ADR. Keel recommended the exact ticket filename, Accepted status, the house ADR header, the reflection-to-typed-query decision, and a conditional transitional-compatibility section governed by Q4/Q5.

**Patron ruling:** The implementer writes `docs/adr/ADR-0009.md` at the ticket's exact path with `Status: Accepted`, ticket/date metadata, and concise Context, Decision, and Consequences. It records the typed Core model, closed sort Enumerations, validator, URL codec and equality, pinned status values, and nulls-last rule. It identifies the legacy reflection helpers being removed. A short compatibility/retirement section is conditional on the Q4/Q5 owner answer and must name DEV-298/DEV-299 and the PostgreSQL-only translation evidence if approved. `Accepted` in the document is the proposed decision status; the owner accepts the PR by merging it. Keel records these requirements in `brief.md`; Patron does not author the ADR in Phase A.

- DEV-297:28-29 and 36 names the exact ADR path and accepted outcome; `specs/PRODUCT.md` §3 makes the ticket authoritative.
- Constitution 1.2.0 §III and Documentation Rules require the typed query decision and a one-page Accepted ADR; `docs/adr/ADR-0010.md` supplies the current house format.
- Q1–Q8 in this artifact define the decision content; Q4/Q5 owner checkboxes govern any transitional departure.

---

## Q10 — Grill close, scope, gates, and rounds

Keel proposed closing the grill after Q1–Q10 rulings and a matching `brief.md`, with the Q4/Q5 owner checkbox on the spec PR. Keel proposed a task sequence from packages and Core types through validation, tests, conditional Web replacement, ADR, and gates; three Spec Kit analyze/fix rounds and two delivery review rounds.

**Patron ruling:** Close the grill only when every asked question is recorded here and mirrored in Keel's `brief.md`, including the combined Q4/Q5 owner checkbox. The checkbox keeps Gate 1 closed until the owner answers it. Freeze the ticket's named model, validator, FsCheck property, UnitTests project, EnrichmentStatus conversion, reflection removal, ADR, and directly required tests; the Web compatibility implementation is conditional on the owner answer. Anything else is a follow-up issue, not a finding in this round. Sequence characterization tests before replacing the legacy helpers, then verify the changed behavior explicitly. At review, verified Critical/High findings always block; in-scope Medium behavior, spec, or gate defects block; Low and non-blocking Medium maintenance observations go to follow-ups. Preserve the standing three Spec Kit analyze/fix rounds, two review rounds, and two remediation commits per round.

- DEV-297:9-36 fixes the deliverables; `task-pipeline`:23-27,61-76 fixes the grill cap, brief handoff, and review/remediation caps; `specs/DEV-296/brief.md`:72-75 records the existing three-analyze-round convention.
- `task-pipeline`:86-95 lists the phase-specific native gates, including property tests, vulnerable packages, format, full tests, and conditional Web gates; the AGENTS.md verification contract distinguishes blocking scope-empty SKIPPED from non-blocking configured OPT-OUT.
- The `code-review` skill severity scale and close rule classify verified findings; constitution 1.2.0 PR Quality Gates and Testing Matrix require applicable tests and mutation evidence. A gate that has not run is not a pass.

---

## Q11 — Legacy request validation boundary

Keel asked whether a new `LegacyMovieBrowseQuery` record and validator should reject nonpositive ids/year/page size and text longer than 200 characters, then return an empty listing rather than an error. This would keep the MVC status response but change totals for invalid page sizes; Keel proposed tests for those new rules.

**Patron ruling:** Do not introduce `LegacyMovieBrowseQuery` or a new Web validator. Adapt the existing `QueryParams` and `MoviesFilterViewModel` directly through a closed `LegacyMovieSort` parser and explicit predicates under the conditional Q4/Q5 Web bridge. Preserve the existing page, id, year, and text request behavior, with the previously ruled unknown-sort fallback and nulls-last refinements. The proposed empty-list validation response contradicts constitution §V's `422` rule, and a 200-character cap is not specified by DEV-297. No new validation failures or thresholds are introduced. `MovieQueryValidator` remains required for the canonical typed model. The combined owner checkbox must also disclose that the transitional MVC path does not use the canonical handler validation decorator; DEV-299 owns its retirement.

- DEV-297:12-27 names the canonical query and its validation rules, without a legacy request-shape change; recon-DEV-297:175-189 documents current MVC binding and behavior.
- Constitution 1.2.0 §V, lines 196-199, requires validation failures to return `422` via the decorator; the Q11 empty-list proposal cannot be treated as constitution compliant.
- `specs/PRODUCT.md` §5 sends a necessary transitional constitution departure to the owner, while keeping the ticket's fixed field set and acceptance criteria intact.

- [ ] **Owner decision for spec PR, combined with Q4/Q5:** May DEV-297 use the temporary explicit Web compatibility path over existing MVC request types, without the canonical handler validation decorator, until DEV-298/DEV-299 migrate browse? The path replaces reflection now, preserves the wider legacy UI fields, and leaves canonical `MovieQuery` for DEV-299.
