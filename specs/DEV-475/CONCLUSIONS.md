# DEV-475 grill conclusions

Authoritative facts: recon-DEV-475, HEAD e06587031dae1d974a41ee98e7b23aa291107129. Decision owner: Patron. Append-only complete exchanges. Grill cap: 12 questions.

---

## Q1 — Delivery boundary

**Keel question:** [from Keel] DEV-475 grill Q1/12: What delivery boundary should this cleanup freeze? Recon-DEV-475 lines 34-38 authorizes dead aliases/exports and libraryUrl/test deletion, shared openMovie without fake popstate, role/cast and sort/direction cleanup, three component splits, Tailwind adopt-or-remove, and eslint-plugin-jsx-a11y; Done when permits splitting follow-ups at spec time. Recommend this delivery include all named cleanup plus jsx-a11y, with Tailwind disposition as an explicit follow-up under that permission: recon only says apparently unused, so adopting/removing it now lacks confirmed styling-usage evidence and adds broad visual regression risk. Cost: one recorded follow-up and no Tailwind change in this delivery; preserve no-behavior-change. Confirm boundary or rule otherwise with cited basis. We will record each answer immediately in specs/DEV-475/CONCLUSIONS.md. Please answer this one decision only.

**Patron answer:** Confirmed: freeze DEV-475 delivery to all ticket-named cleanups (dead aliases/exports and web/src/library/libraryUrl.ts + libraryUrl.test.ts deletion, shared openMovie without synthetic popstate, role/cast and sort/direction literal-typing cleanup, three component splits) plus eslint-plugin-jsx-a11y, and split Tailwind adopt-or-remove into a recorded follow-up preserving no-behavior-change (basis: DEV-475 Wanted and Done-when in recon-DEV-475 lines 36-38; specs/PRODUCT.md section 5 lines 43 and 48; constitution.md lines 760-761).

**Rationale and implications:** Ticket permits splitting follow-ups at spec time. Tailwind remains unchanged in this delivery; Patron requests a recorded follow-up, to be recorded by Rigger. All other named work remains required. No ticket contradiction or constitution departure has been ruled necessary.

---

## Q2 — Navigation ownership

**Keel question:** [from Keel] DEV-475 grill Q2/12: What navigation ownership should replace the three fake-popstate producers? Recommend a single feature-local imperative navigation entry point beside useAppNavigation, with shared openMovie used by details/RelatedMovies and the existing player flow sharing its history-and-notification mechanism. Keep native popstate solely for browser Back/Forward; programmatic push/replace explicitly notifies subscribers through the same internal resync path, with cleanup and no duplicate history entries. Preserve existing route parsing, return URL, player intent and filter/library URL semantics. No router dependency, global state library or new architectural layer. Basis: ticket shared openMovie/no fake popstate (recon 37), current producers/listeners/tests (13), PRODUCT no speculative abstractions (43). Cost: update all named listeners/call sites together and test both navigation sources. Confirm this ownership and synchronization contract or specify a simpler compatible alternative.

**Patron answer:** Confirmed: replace synthetic popstate producers with a feature-local imperative navigation entry point beside useAppNavigation exposing shared openMovie and a shared programmatic push/replace subscriber-notification path, keeping native popstate strictly for browser Back/Forward with no new router/state dependency or layer (basis: DEV-475 Wanted in recon-DEV-475 lines 13 and 37-38; specs/PRODUCT.md lines 18-19 and section 5 lines 43-44).

**Rationale and implications:** One feature-local history/notification mechanism serves programmatic navigation. Native popstate retains browser traversal semantics. Consumer cleanup, return URLs, player intent, routes and URL filtering must remain behaviorally equivalent; new router/state dependencies and architecture layers are excluded.

---

## Q3 — Component and file boundary

**Keel question:** [from Keel] DEV-475 grill Q3/12: What component extraction boundary counts as completing the three required splits? Recommend retain FilterPanel.tsx, LibraryView.tsx and MovieDetailsView.tsx as orchestration shells and extract cohesive rendered sections and their local behavior into sibling modules under their existing feature/library directories. Keep one owner per state/effect, explicit typed props and existing generated API types; avoid generic services, prop-forwarding-only wrappers, line-count targets, UI redesign or BrowserPlayer changes. Quill must enumerate concrete extracted modules and responsibility boundaries in plan/tasks, based on subsequent bounded recon rather than guessed internal sections. Existing App.tsx, useAppNavigation.ts, RelatedMovies.tsx, filterQuery.ts and associated tests can change only for the named cleanup/navigation integration. Basis: recon 12-14, ticket 36-38, PRODUCT 18-19/section 5. Cost: local module moves and behavioral regression coverage. Confirm this extraction/file boundary, including deliberate permission to refactor the named integration files without unrelated rewrites?

**Patron answer:** Confirmed: keep FilterPanel.tsx, LibraryView.tsx, and MovieDetailsView.tsx as orchestration shells extracting cohesive sections into sibling modules under existing feature directories (enumerated in plan/tasks from bounded recon, using generated API types with no new layers or line-count targets), and authorize bounded edits to App.tsx, useAppNavigation.ts, RelatedMovies.tsx, filterQuery.ts, and associated tests strictly for the named cleanup and navigation integration (basis: DEV-475 Wanted/Done-when in recon-DEV-475 lines 12-14 and 36-38; specs/PRODUCT.md lines 18-19 and section 5 lines 44, 46, and 48).

**Rationale and implications:** Cohesive extractions preserve state/effect ownership and UI behavior. Concrete sibling filenames require bounded recon before plan completion; no arbitrary file-length bar or BrowserPlayer cleanup is authorized. Permission for named integration files is bounded to this work.

---

## Q4 — Deletion evidence

**Keel question:** [from Keel] DEV-475 grill Q4/12: What proof governs dead-code deletion? Recommend delete the explicitly named libraryUrl.ts and libraryUrl.test.ts and filterQuery alias/shim surface only after a bounded reference/export inventory confirms current consumers; preserve canonical live filterQuery behavior/tests. For any other export, record symbol, defining path, complete relevant importer/reference evidence including barrels, tests and dynamic references before removing its export or implementation. Own-test-only references support orphan removal but do not prove the code is dead across all configurations. Whole-file deletion beyond the ticket-named pair requires a separate cited Patron ruling before editing; uncertainty requests recon rather than a speculative deletion. No replacement aliases or tests for intentionally removed orphan code. Basis: ticket 36-38, recon limits 11-12/23-24, PRODUCT section 5 line 48. Cost: bounded inventory and possibly retain uncertain symbols pending recon. Confirm this deletion acceptance rule?

**Patron answer:** Confirmed: govern dead-code deletion by bounded reference/export inventory (including barrels, tests, and dynamic references) for ticket-named libraryUrl.ts, libraryUrl.test.ts, filterQuery.ts alias/shim exports, and unreferenced exports, preserving canonical live filterQuery behavior/tests with no replacement aliases, while requiring a separate cited Patron ruling before deleting any tracked file beyond the ticket-named pair and routing uncertainty to recon (basis: DEV-475 Wanted in recon-DEV-475 lines 11-12, 23-24, and 36-38; specs/PRODUCT.md section 5 line 48; Charter section 2.3 item 6).

**Rationale and implications:** The ticket names the orphan pair for deletion but does not establish complete reference evidence. Do not replace dead aliases. Preserve live canonical filtering tests; no unruled whole-file deletions.

---

## Q5 — Type cleanup compatibility

**Keel question:** [from Keel] DEV-475 grill Q5/12: What behavior contract applies to role/cast removal and sort/direction typing? Recommend remove the ticket-named role parameter and actor casts from the live library path, use existing generated API role types at actual API boundaries, and define sort/direction as the existing allowed literal values rather than widened strings or assertions. Preserve current allowed choices, defaults, URL parsing/serialization, invalid-value fallback, filtering and request shapes; runtime parsing still validates untrusted URL values. No hand-rolled API DTO duplicate or OpenAPI change. Basis: ticket 36-38, recon canonical filtering 12, PRODUCT contract chain 42, constitution strict TS 48. Cost: update direct callers and behavior tests, no user-visible option changes. Confirm this compatibility contract?

**Patron answer:** Confirmed: remove the ticket-named role parameter and actor casts from the live library path, consume generated API role types at actual API boundaries without hand-rolled DTO duplicates or OpenAPI changes, and literal-type sort/direction to existing allowed values while preserving runtime URL validation, defaults, invalid-value fallback, and request shapes (basis: DEV-475 Wanted/Done-when in recon-DEV-475 lines 12 and 36-38; specs/PRODUCT.md lines 8-12 and 42; constitution.md lines 760-761; Charter section 2.3 contract chain).

**Rationale and implications:** Strict literal typing does not replace runtime validation. Preserve options, defaults, fallback, filters, URLs and API requests. Generated contract files remain outside this refactor.

---

## Q6 — Accessibility lint dependency

**Keel question:** [from Keel] DEV-475 grill Q6/12: What is the bounded jsx-a11y adoption policy? Recommend deliberately approve the ticket-requested eslint-plugin-jsx-a11y dependency, wire its supported recommended rules into the existing web lint configuration, and use the existing package manager/lockfile and repo-compatible version resolved during bounded implementation recon. Fix actionable violations without blanket disable directives, lowering severities or broad lint exclusions. Preserve layout and interaction while improving required semantics; unrelated redesign or a newly required dependency needs a separate ruling. Any whole-file rewrite outside the named ticket files needs explicit approval under section 2.3. Basis: ticket 37-38 explicitly requests plugin, PRODUCT section 5 line 43, constitution web stack 48. Cost: dependency/lock/config edits and any rule-triggered markup changes in an enumerated lint-violation inventory; this inventory must precede final plan freeze. Confirm this policy and bounded package/config edits?

**Patron answer:** Confirmed: approve adding eslint-plugin-jsx-a11y (repo-compatible version via existing package manager/lockfile) wired with its recommended rules in the existing web ESLint config, and authorize bounded package/lockfile/config edits plus semantic fixes to files in an enumerated violation inventory (preserving layout/interaction with no blanket disables, lowered severities, or broad exclusions; any extra dependency or whole-file rewrite outside named files requires a separate section 2.3 ruling; basis: DEV-475 Wanted/Done-when in recon-DEV-475 lines 37-38; specs/PRODUCT.md section 5 lines 43 and 48; constitution.md lines 760-761).

**Rationale and implications:** Ticket already requests the dependency; this records deliberate structural approval. Version, actual configuration paths and violation file inventory require bounded recon before final plan freeze. No weakening of lint rules or unbounded UI redesign.

---

## Q7 — Test strategy and task ordering

**Keel question:** [from Keel] DEV-475 grill Q7/12: What verification and task ordering establishes no behavior changes? Recommend (1) bounded inventories and baseline existing web gates, (2) regression characterization where existing coverage is insufficient, (3) orphan/alias and type cleanup, (4) shared navigation with all consumers migrated together, (5) three cohesive extractions, (6) jsx-a11y wiring and inventoried fixes, (7) final web verification. Required evidence is existing lint, typecheck, unit and e2e commands all passing, with targeted RTL/Vitest tests for push/replace updates without fake popstate, native Back/Forward resync, history count/return URL/player intent, library filter/sort/default/fallback behavior, details/related-movie navigation and listener cleanup; existing e2e traversal/library/details/player smoke coverage must run or be extended where missing. Test-dispatched popstate may simulate native traversal but cannot substitute for exercising the new programmatic path; do not prohibit tests that simulate native events. No new test framework, no arbitrary line-count tests. C# gates remain scope-dependent and SKIPPED stays SKIPPED; unrunnable e2e is Could not run and blocks Done-when. Basis recon 12-15/38/48 and task-note baseline receipts. Confirm this test/order/acceptance policy?

**Patron answer:** Confirmed: adopt the 7-step task ordering and verification contract requiring existing web lint, typecheck, Vitest/RTL unit tests (covering both programmatic navigation without synthetic popstate and simulated native Back/Forward popstate traversal, history/return-URL/player intent, filter/sort/fallback, and listener cleanup), and e2e smoke suites to pass with no new test framework or line-count assertions, keeping scope-empty C# gates as SKIPPED and treating unrunnable e2e as Could not run blocking Done-when (basis: DEV-475 Done-when in recon-DEV-475 lines 12-15 and 38; constitution.md lines 760-761; task note DEV-475 lines 15-20; AGENTS.md Verification commands).

**Rationale and implications:** Existing test coverage is evidence of available seams, not a passing runtime receipt. Both native traversal and imperative navigation need independent coverage. All four ticket-required web verification lanes must run successfully; no baseline scope-empty skip counts as acceptance.

---

## Q8 — Closing bar and loop discipline

**Keel question:** [from Keel] DEV-475 grill Q8/12: What exact review closing bar applies within the Conductor-mandated cap? Recommend zero unresolved in-scope Critical/High/Medium/Low findings: each must be fixed or explicitly rejected with cited evidence; accepted Low cannot be silently waived. On this size L task require all Sentry/Risk, Ledger/Standards and Compass/Spec reports before adjudication, with findings pinned to reviewed head and posted to PR. Unnecessary abstractions are follow-ups, not extra fix work. Maximum 2 review rounds and 2 fix commits per round; exhausted cap with blocking findings stops as blocked for Conductor/Patron disposition, never an automatic third round or merge clearance. Frozen scope ends: anything else is a follow-up issue, not a finding in this round. Gates must satisfy Q7 separately; no merge or owner Gate 1 claim follows from grill closure. Basis assigned role review rules, PRODUCT bounded scope and manual merge, Conductor cap. Confirm this closing bar?

**Patron answer:** Confirmed: require zero unresolved in-scope Critical/High/Medium/Low findings (fixed or explicitly rejected with cited evidence, no silent Low waivers), all three challenger reports (Sentry/Risk, Ledger/Standards, Compass/Spec) pinned to reviewed HEAD and posted to PR on this size:L task, max 2 review rounds and 2 fix commits per round (halting as blocked if exhausted), and strict frozen scope routing out-of-scope items or unnecessary abstractions to follow-ups, with Q7 gates satisfied separately and no merge or Gate 1 clearance from grill closure (basis: Charter section 2.3 standing review/scope/merge rules; specs/PRODUCT.md lines 18-19; task note DEV-475 line 4).

**Rationale and implications:** No unresolved accepted severity is waived by elapsed rounds. Require complete axis evidence and explicit dispositions; cap exhaustion blocks. Scope is frozen: anything else is a follow-up issue, not a finding in this round.

---

## Q9 — ADR and grill closure

**Keel question:** [from Keel] DEV-475 grill Q9/12, proposed closure: Confirm ADR 0042 outline and shared understanding for closing this grill. Recommend docs/adr/0042-explicit-spa-navigation-and-cleanup-boundaries.md, status proposed: feature-local imperative openMovie/history notifications share one resync contract; native popstate is browser traversal only; reject fake native events and adding a router/state abstraction for this cleanup; preserve route/history/filter/player behavior, with cohesive sibling extractions and evidence-led orphan removal. Cite Q1-Q8 and ticket/PRODUCT/constitution recon. Keep Tailwind disposition an explicitly recorded follow-up for Rigger; no other ticket work deferred. No new domain terminology, so no CONTEXT glossary changes. brief.md will include all complete Q&A, frozen scope, file families, seven-step ordering, tests, review bar, caps and this ADR outline. Exact extracted module names, full reference inventory, a11y violations/config paths and compatible package version remain bounded recon prerequisites for Quill plan completion/freeze, not facts asserted by the grill; Quill must request recon instead of guessing. No owner structural escalation identified by our rulings. Does this settle the grill and authorize writing brief.md, ASSUMPTIONS.md and proposed ADR 0042 (CONCLUSIONS already append-only), while keeping plan freeze/Gate 1/implementation unclaimed?

**Patron answer:** Confirmed: grill settled on Q1-Q8; approve proposed ADR 0042 (docs/adr/0042-explicit-spa-navigation-and-cleanup-boundaries.md, status proposed, no CONTEXT.md glossary changes) and authorize writing brief.md, ASSUMPTIONS.md, and ADR 0042 while recording Tailwind disposition as a follow-up for Rigger, requiring bounded recon for exact extracted module names/reference inventory/a11y violations/package version before Quill freezes the plan, with no owner structural escalation and plan freeze/Gate 1/implementation unclaimed (basis: Q1-Q8 rulings; recon-DEV-475 lines 36-38, 43, 45, and 48-53; constitution.md lines 911-912).

**Rationale and implications:** Grill closes at 9/12 questions. ADR is proposed, not accepted by owner merge. Quill can draft from this brief but must obtain bounded recon before finalizing exact plan files and freezing. Tailwind is the sole explicitly deferred ticket work; recording belongs to Rigger. No new glossary terms or owner checkbox identified.

---

## Q10 — Post-recon analyze rulings (jsx-a11y disposition and sibling boundaries)

**Conductor / Keel ask:** [from Conductor] DEV-475 product and design rulings: 1) eslint-plugin-jsx-a11y 6.10.2 only supports ESLint up to v9 while repo uses ESLint 10.14.0; rule whether to adopt via flat config compat or defer to follow-up; 2) Ratify proposed sibling components and state boundaries in recon-DEV-475 lines 23-25 for FilterPanel, LibraryView, and MovieDetailsView.

**Patron answer:**
1. **Defer `eslint-plugin-jsx-a11y` to a recorded follow-up ticket** (superseding Q1/Q6 on in-delivery jsx-a11y adoption): do not adopt `eslint-plugin-jsx-a11y` 6.10.2 via `@eslint/compat` or peer-dependency overrides against ESLint 10.14.0 in DEV-475, and split jsx-a11y adoption into a recorded follow-up once upstream declares ESLint 10 support or a dedicated compatibility spike is scoped (basis: `recon-DEV-475` lines 14–16 proving v6.10.2 declares peer `eslint ^3 || ... || ^9` excluding ESLint 10.14.0 with no verified supported pairing; DEV-475 Done-when permission to split follow-ups at spec time in `recon-DEV-475` line 38; `specs/PRODUCT.md` §5 lines 43 and 48; Charter §2.3 item 1).
2. **Ratified:** approve the concrete sibling components and state/effect ownership boundaries in `recon-DEV-475` lines 23–25 for `FilterPanel.tsx` (`DualRangeField.tsx`, `FilterSearchField.tsx`, `FilterFacetCombobox.tsx`, and cohesive `FilterSortControls.tsx`), `LibraryView.tsx` (`LibraryBrowseView.tsx`, `LibraryFilterSummary.tsx`, `LibraryResultsToolbar.tsx`, `LibraryResultsBody.tsx`), and `MovieDetailsView.tsx` (`MoviePoster.tsx`, `ManualMatchControl.tsx`, `CastSection.tsx`, `MoreWithPanel.tsx`, `MorePosterCard.tsx`, `FileSection.tsx`), keeping parent query/URL/route/data/local-play orchestration in the shells and local UI/draft/failure state in the siblings (basis: `recon-DEV-475` lines 23–25; Q3 ruling in `CONCLUSIONS.md`; `specs/PRODUCT.md` §5 lines 44, 46, and 48).

**Rationale and implications:** Adding an unverified `@eslint/compat` dependency or forcing peer overrides on an unsupported ESLint 10 pairing risks brittle lint gates and violates Q6's "repo-compatible version" requirement; splitting to a follow-up is explicitly authorized by the ticket's Done-when clause (`recon-DEV-475` line 38), so no owner checkbox is needed. Rigger is asked to create the jsx-a11y follow-up ticket under DEV-490. With jsx-a11y deferred, the unavailable rule-by-file violation inventory (`recon-DEV-475` lines 18–20) is no longer a prerequisite for DEV-475 plan freeze. The ratified sibling component names and state boundaries from `recon-DEV-475` lines 23–25 satisfy the Q3/Q9 component-seam prerequisite.

---

## Q11 — Navigation sibling naming

**Keel question:** [from Keel] DEV-475 navigation sibling naming decision: Conductor requests settled naming in the synchronized brief and ADR. Q2 settles ownership beside useAppNavigation, but Q10 and current recon do not name that sibling. Recommend web/src/features/app-navigation/appNavigation.ts for shared openMovie and the shared programmatic push/replace notification/subscription mechanism, with useAppNavigation.ts retaining React route-state subscription and native Back/Forward handling. This is a filename decision within Q2 existing feature-local boundary, with no new layer or dependency. Basis Q2/Q3 and PRODUCT section 5. Please confirm this exact path or supply the settled alternative, and record the cited ruling in CONCLUSIONS.md. Report back to Keel when recorded.

**Patron answer:** Confirmed: name the feature-local imperative navigation sibling `web/src/features/app-navigation/appNavigation.ts` to own shared `openMovie` and the shared programmatic `pushState`/`replaceState` subscriber-notification mechanism, while `web/src/features/app-navigation/useAppNavigation.ts` retains React route-state subscription and native browser Back/Forward `popstate` handling (basis: Q2 and Q3 rulings in `specs/DEV-475/CONCLUSIONS.md`; `recon-DEV-475` lines 13, 21, and 37–38; `specs/PRODUCT.md` lines 18–19 and §5 lines 43–44).

**Rationale and implications:** `web/src/features/app-navigation/appNavigation.ts` sits directly alongside `useAppNavigation.ts` within the existing `app-navigation` feature directory, introducing no new folder, architectural layer, or dependency. Brief, ADR 0042, spec, plan, and tasks may reference this exact path as the settled imperative navigation module.


