# DEV-297 conclusions

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
