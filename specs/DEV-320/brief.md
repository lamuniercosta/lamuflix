# DEV-320 - Phase 2 grill brief (CLOSED)

## Identity and state

- Worktree: F:/Dev/LamuFlix.worktrees/feature-320-spec
- Branch: feature/320-spec
- Intake HEAD: cb6fe48996cd16027f5c0496876ec5c86d97f7a2
- Current HEAD after Patron Q1 receipt: 76a27a97e041546beb5e026f2c05a15540e1505f
- Size M; no ui tag. Grill closed at Q12/12; Q1-Q12 ACCEPT or ACCEPT amended. No further questions remain.
- Grill decisions settled; Quill drafts now exist. Technical pin selection is BLOCKED by the post-draft evidence decision below. This is not plan freeze, Gate 1, or authorization for implementation or gates. Later Q7-Q12 rulings supersede the explicitly historical pending states below.
- Preserve existing .specify/feature.json modification and untracked recon-DEV-320.md.

## Authority

Ticket description is verbatim in recon-DEV-320:15-30: source identity 15-16; Overview 18-19; Scope and Technical Design 20-26; Acceptance Criteria 27-29. Other sources: recon-DEV-320:1-14, task-pipeline Phase 2, lamuflix-team-charter, and this worktree specs/PRODUCT.md.

## Q1/12 - Accepted contract boundary and generation approach

Question: Confirm the contract boundary and generation approach? Use committed web/src/api/openapi.json as the sole client-generation input; generate web/src/api/types.ts using openapi-typescript; provide web/src/api/client.ts as a small openapi-fetch runtime consuming generated types; MSW consumes that same generated contract. No backend DTO, OpenAPI route/status/schema change, duplicated handwritten types, or endpoint-wrapper layer. Cost: regeneration whenever committed OpenAPI changes. Interpret the ticket phrase generate client.ts as the runtime client backed by generated types.ts, without changing deliverables.

Patron: ACCEPT; no ticket contradiction or constitution departure. Ticket recon lines 22-23 explicitly name both dependencies; charter line 24 requires the contract chain. Handlers are pinned to web/src/mocks/handlers.ts and consume generated types only (ticket line 25; charter line 24). Fixtures are authored data, not handwritten contract types. No handler-generation dependency is approved. The play endpoint is mock-only; no local execution occurs (recon line 14).

Complete exchange and cited ruling: CONCLUSIONS.md, Q1/12. Patron additionally cited constitution lines 335, 337, 470-472; exact clause evidence must be added to recon before resolving generation/drift enforcement.

## Settled approach and file decisions

- Input only: web/src/api/openapi.json.
- Generate: web/src/api/types.ts.
- Add thin typed runtime client: web/src/api/client.ts.
- Add generated-type mock handlers: web/src/mocks/handlers.ts.
- Do not add handler-generation dependencies or execute local playback.
- Q2-Q6 below settle handler generation, fixtures, client origin, mock scenarios, test lifecycle and development bootstrap. Generation/drift wiring, final file set, complete test strategy, gate expectations, task ordering and loop discipline are settled in Q8-Q12 below.

## Loop discipline - settled in Q11

Phase 2 question cap: 12, per Conductor instruction and task-pipeline line 25.
Closing bar, frozen review scope and round terms received the cited Q11 ruling; see Q11 below. No defaults were silently adopted.

## Q2 recon received - resolved

Provide line-citable facts for the committed OpenAPI operation inventory (method/path, parameters and headers, request bodies, response statuses and schemas), current MSW/Vitest lifecycle and development bootstrap/configuration including client base URL conventions, existing generation/drift-check and web-gate wiring, and exact applicable constitution clauses (including lines 335, 337, 470-472 cited by Patron). Evidence must come from the named worktree and be persisted in recon-DEV-320; no tests, gates, or edits are requested of recon. Then resume Q2, with 11 questions remaining.

## Process receipt

Patron committed the Q1 conclusions path despite the ask forbidding commits. Read-back confirms commit 76a27a97e041546beb5e026f2c05a15540e1505f, subject DEV-320 - Patron Q1 ruling: contract boundary and generation approach. Preserve this unexpected commit; Keel made no commit. Report the head change to Bernstein before resumption.

## Q2/12 - ACCEPT amended: generated handlers, authored fixtures

The full Q2 exchange is appended to CONCLUSIONS.md. Generate committed web/src/mocks/handlers.ts from committed OpenAPI with one handler per operation. Authored web/src/mocks/fixtures.ts supplies generated-type fixture data/scenario selection only, never handwritten contract types or handler registration. Generator lives under web/ on plain Node ESM or existing toolchain, adds no dependency, and is wired through package.json. Output deterministic, marked generated, never hand-edited. Require handler regeneration drift proof and per-operation coverage. Supporting files are ticket-deliverable support, not a new layer. No ticket contradiction or constitution departure (constitution 470-472; ticket recon 25; charter 17). Recon 31-68 now supplies Q2 facts; the earlier stop request is historical and resolved.
## Q3/12 - ACCEPT amended: client origin and unchanged contract

Export explicit-baseUrl client factory plus default client using optional VITE_API_BASE_URL or browser origin; tests use https://test.local. Generated MSW matches paths on any origin. Add optional ImportMetaEnv typing in web/src/vite-env.d.ts and one README line; no committed .env. Preserve bodyless 202/204, generated ProblemDetails, number|string unions/nullability. Location remains available on raw response; undeclared OpenAPI Location header is noted, no ticket. No proxy, port guess, auth/retry/wrappers or schema repair. Basis: Q1, Q2, shared recon note 33-44 and 51-52, charter 24, constitution 466 and 471-472. Patron read a separate recon file; those file-reference claims do not supersede the authoritative shared note. Full exchange in CONCLUSIONS.md.

## Q4/12 - ACCEPT amended: deterministic mock behavior

Immutable generated-type fixtures; one success default for all nine operations. Browse static page, no query engine; details complete movie plus null-metadata/null-field variant; genres/people arrays. No state, filesystem, process or business-rule simulation. Bodyless 202/204; 202 Location opaque fixture under existing route. Ready overrides cover every declared error for each operation, only declared statuses, with application/problem+json ProblemDetails. ResetHandlers and immutable fixtures isolate tests. Basis ticket 25-29; shared recon 33-44,49; constitution 466. Full exchange in CONCLUSIONS.md.

## Q5/12 - ACCEPT amended: global test lifecycle

server.ts exports setupServer with generated API and existing harness handlers; all lifecycle hooks move to sole configured setup.ts alongside RTL cleanup, registered once. Existing msw.test.ts passes unchanged (import-path-only adjustment permitted). Add client.test.ts and mocks/handlers.test.ts; prove strict unhandled API rejection. Exact MSW option is not decided: Patron explicitly requires official MSW documentation confirmation at plan time, with no guessed option. Outcome ruling is strict rejection; existing onUnhandledFrame setting is not proof of HTTP strictness. Integration edits forced by AC29; no new architecture. Full exchange in CONCLUSIONS.md.

## Q6/12 - ACCEPT amended: opt-in browser worker

Ticket26 requires development without backend. Worker starts through dynamic browser.ts import only for DEV and VITE_ENABLE_MSW=true, awaited before createRoot. Failure is visible with no silent fallback. Production never registers; copied inert worker asset accepted. Same generated handlers, fixtures, wildcard origins and strict API outcome. Add optional flag typing and README documentation. Generate public/mockServiceWorker.js using installed MSW CLI, never hand-edit; regeneration on MSW upgrade. Exclude official worker from eslint/prettier, and generated types/handlers only if generation is not gate-clean. These tracked config edits are forced by the web gate. Any package.json MSW-init field is permitted only if official CLI emits it; CLI behavior is not yet evidenced. Basis ticket26; shared recon46,51,57-59; Q2-Q5. Full exchange in CONCLUSIONS.md.

## Historical stop before Q7 - recon resolved by Q7

Provide official MSW 3.0.2 documentation-backed facts, persisted in the shared recon-DEV-320 NOTE from the named worktree:
1. Exact Node setupServer.listen and browser setupWorker.start option/callback semantics that fail unhandled HTTP /api requests while allowing unrelated browser assets. Does existing onUnhandledFrame:error apply to HTTP or require a different policy? Record the official API evidence and installed-version applicability; do not infer from the current setting.
2. Exact official CLI invocation for generating web/public/mockServiceWorker.js with installed MSW, and whether initialization writes package.json metadata or any other file. Record the file effects without changing project files.

These are required to freeze strict mock behavior and the supporting file set. Q5/Q6 explicitly left them unverified. No further question is asked until recon answers; Q1-Q6 are settled, 6 questions remain. Brief OPEN: not frozen or ready for Quill. No Spec Kit drafting, production edits, commits or gates. Existing HEAD and unrelated changes preserved.
## Q7/12 - ACCEPT amended: exact MSW policy and init

Shared recon68-78 resolves the preceding stop; it is historical, no longer current. Node server.listen keeps onUnhandledFrame:error for every unhandled frame, preventing real-network fallback. Worker alone uses web/src/mocks/policy.ts: HTTP pathname /api or /api/ prefix calls defaults.error before any bypass; other HTTP and WebSocket frames bypass. No common-asset helper needed. Unit coverage: /api, /api/x.json, /apiculture, asset path, ws frame; server test proves unhandled API failure. Explicitly create web/public; from web run installed npx msw init ./public --save; accept documented package.json msw.workerDirectory and automatic worker refresh. Await worker startup; asset exclusions and no hand-editing stand. Supporting files forced by ticket26/AC29. No ticket contradiction or constitution departure. Full question and cited ruling appended to CONCLUSIONS.md. Seven questions settled; five remain; brief OPEN pending Q8 onward.

## Q8/12 - ACCEPT amended: generation and drift

Exact-pinned openapi-typescript devDependency and openapi-fetch runtime dependency; official compatibility lookup at implementation selects versions, lockfile in scope. Plain Node web/scripts/generate-api.mjs and check-api.mjs; generate:api writes generated types.ts and handlers.ts from committed OpenAPI; check:api generates to temp, byte-compares LF output, never overwrites committed files. lint runs check:api before eslint/prettier, leveraging existing CI and local web gate. ci.yml, run-web-gates.ps1 and harness.yml remain untouched. Generated types/handlers ignored by prettier/eslint, still checked by tsc. Q8 replaces Q2 Vitest drift comparison with check:api; Vitest retains generator determinism, coverage and stale-temp drift rejection. README records backend-then-web regeneration and commands. Basis constitution337,470-472, PRODUCT12, recon53-60, ticket22/28; no departure. Full exchange in CONCLUSIONS.md. Eight questions settled; four remain.

## Q9/12 - ACCEPT amended: proof strategy

Runtime client tests: all nine operations, path/query/body serialization, success/error payloads, raw Location, bodyless202/204, explicit/default origins. client.typecheck.ts under src (never runtime-imported) has positive typed calls for all nine, preserves id union distinctions, and @ts-expect-error negatives for bad paths, required parameters/body and shapes. handlers.test.ts: every operation success and declared error/status/content-type, nullable fixture, override/reset isolation. policy.test.ts: Q7 cases. Extract testable DEV/flag startup to mocks/startup.ts, awaited by main.tsx; startup.test.ts uses existing Vitest env/module tools for startup-before-render, off defaults/production and visible failure. Put generator test at web/src/mocks/generate-api.test.ts to ensure src typecheck and Vitest discovery; test deterministic LF, operation coverage and stale-temp rejection. Manual browser smoke with backend stopped and typed worker response is recorded once in PR verification notes, supplementary and not a gate. No new framework, backend change or fabricated browser proof. Full exchange in CONCLUSIONS.md. Nine questions settled; three remain.

## Q10/12 - ACCEPT amended: frozen file set and task ordering

All paths below relative to worktree. Frozen implementation set:
- web/package.json; web/package-lock.json
- web/scripts/generate-api.mjs; web/scripts/check-api.mjs
- web/src/api/types.ts (generated); web/src/api/client.ts; web/src/api/client.test.ts; web/src/api/client.typecheck.ts
- web/src/mocks/handlers.ts (generated); fixtures.ts; browser.ts; policy.ts; startup.ts; handlers.test.ts; policy.test.ts; startup.test.ts; generate-api.test.ts (all under web/src/mocks/)
- web/src/mocks/generate-api.d.ts only if compiler requires script declarations, never API types
- web/src/vite-env.d.ts; web/src/test/setup.ts; web/src/test/server.ts; web/src/main.tsx
- web/public/mockServiceWorker.js (official CLI); web/eslint.config.js; web/.prettierignore; web/README.md

No tsconfig*, vite.config, .gitattributes, msw.test.ts, App/UI, backend, committed OpenAPI, harness/workflow, ADR or glossary change. Compiler need beyond conditional .d.ts returns to Patron. package.json edits limited to approved dependencies/scripts and workerDirectory.

Task order: phase1 contract tooling/types/client with compile/runtime proof and types drift immediately active; phase2 fixtures/generated handlers/global server lifecycle/scenario proof, extending same drift check to handlers; phase3 browser policy/startup/official worker/bootstrap proof; phase4 final documentation and integration verification plus supplementary manual smoke receipt. Each phase includes required ignore entries and README changes and ends green on existing web gate (lint/tsc/test:run/build). No generated file lands unchecked. Phase1 client tests may use explicit local test overrides until phase2 global generated handlers exist; no backend dependence. Single writer per file; Anvil creates new modules, Cog alters existing integration/config, Conductor sequences handoffs. Full exchange in CONCLUSIONS.md. Ten questions settled; two remain.

## Q11/12 - ACCEPT amended: gates and loop discipline

Closing bar: evidence-supported in-scope Critical/High/Medium block; three axis reports mandatory for M, file:line verification and posted/read-back PR receipt. Frozen review scope: generated typed client, generated MSW handlers and typed scenarios, deterministic generation/drift, strict isolated tests and opt-in browser startup with Q10 supporting files; anything else is a follow-up issue, not a finding in this round.

Round cap: two review rounds, at most two fix commits per round; third-round findings go to owner review on PR, no third repair loop. Two numbered Quill correction rounds for drafting/analyze; unresolved mismatch reports blocked to Conductor, never silently freeze. No seat merges. Out-of-scope Critical/High or broken behavior routes to Rigger follow-up (fold existing ticket first); other findings, including Low, get noted, no ticket in CONCLUSIONS.md, no speculative abstraction fix.

Per-phase web gate plus final Gauge task-pipeline verification. Actual exit-2 C# and mutation scope-empty results are SKIPPED (scope-empty), never PASS; applicable unrunnable gate blocks. No threshold/config waiver. Final proof: check:api and generator tests, tsc including compile-only proof, full Vitest unchanged harness tests, lint/build, hosted CI/gitleaks/vulnerability checks. Manual browser smoke is supplementary. propertyTests: opt-out - no domain invariants in generated contracts, static fixtures and bootstrap wiring; Conductor records this exact opt-out in task note before applicable verification. No gates run during grill. Full exchange in CONCLUSIONS.md. Eleven questions settled; one remains.

## Q12/12 - ACCEPT: grill closed and handoff boundary

Patron confirms shared understanding and ratifies all Q1-Q11 decisions. Phase1 uses generated-type local server.use overrides under the existing lifecycle; lifecycle moves exactly once in phase2. Types drift is active phase1, handlers drift joins phase2. Exact dependency-version/API facts go through documentation-backed recon, never guesswork; additional compiler/file decisions return to Patron. No unresolved owner checkbox, ticket contradiction, constitution departure, taste assumption, glossary change or ADR requirement. Full exchange appended in CONCLUSIONS.md.

Ready for Quill to draft spec.md, plan.md and tasks.md from this brief and shared recon; no drafting performed here. Quill decides nothing; gaps return needs decision to Keel. Next is drafting, read-only analysis and bounded corrections, then plan challenge/freeze and spec PR/Gate1 through the Conductor. Preserve HEAD 76a27a97e041546beb5e026f2c05a15540e1505f and existing .specify/feature.json plus recon-DEV-320.md. This ask edited only brief and conclusions; no commits, source changes, Spec Kit files or gates. CONCLUSIONS remains append-only and is recorded in the future authorized spec PR step.

## Post-draft pin and audit decision - CONDITIONALLY CLEARED (2026-10-11)

Authority: shared task note DEV-320:39-41, binding Patron addendum :40 (39a); shared recon-DEV-320:113-143; CONCLUSIONS.md candidate (b) qualification :200-208 and appended Patron audit ruling. This replaces the historical absence-of-evidence pin blocker and discharges the subsequent audit-recon prerequisite; the grill remains closed at Q12/12.

**Decision:** openapi-typescript@6.7.6 (devDependency) and openapi-fetch@0.9.8 (runtime dependency) are cleared for T001 subject to all three binding conditions below. Audit findings are in scope but not a blocker under this ruling. No further Wisp recon and no owner checkbox. This older exact pair preserves the ticket-named packages, frozen TypeScript 6.0.3 / Node 24 toolchain and unchanged committed contract.

**Three binding conditions, verbatim from shared DEV-320:40:**
39a. Patron addendum to line 39 (exact conditions, binding on T001 and plan; source: Patron audit ruling, worktree feature/320-spec HEAD 76a27a97e041546beb5e026f2c05a15540e1505f): (1) generate:api and check:api pass the generator only the committed local file path web/src/api/openapi.json, never a URL or a glob of untrusted input. Basis: generator input is the repo-controlled contract (constitution 470-472); advisories need attacker-controlled glob/WebSocket/multipart input (recon-DEV-320:122-126); Q11 blocks only evidence-supported in-scope High (CONCLUSIONS:142-143; brief:102). (2) plan.md:15 records the older major, the five High + one Moderate dev-only advisories (recon-DEV-320:119-127), this ruling, and that the runtime closure audits 0 (recon:129-131); npm audit --omit=dev is the runtime receipt in T021; a plain npm audit High on these six nodes is known, not a new finding, and any other High is a new finding. Basis: routing paragraph CONCLUSIONS:200-214 (Quill records pins in T001 and older-major/evidence limits in plan). (3) No npm audit fix, no overrides, no --force; the lockfile adds exactly the 29-package closure (recon:115). Basis: overrides prohibited and force conflicts with frozen TS 6.0.3 (recon:135-136; CONCLUSIONS:169-171); package.json/lockfile are in the frozen Q10 file set (CONCLUSIONS:128). Recording: append the full ruling to specs/DEV-320/CONCLUSIONS.md at the authorized Phase A recording step; Keel reconciles brief:114-136.

Evidence limits: observed scratch install, two byte-identical generations and corrected harness tsc validation over all nine operations qualify this exact pair for this contract/toolchain (shared recon:137-139; CONCLUSIONS.md:202-208). General TS 6 support, runtime fetch/MSW behavior, generated-file lint/format compliance, multi-status narrowing and affected-code unreachability are not established (shared recon:140-141). The runtime-closure audit 0 is observed scratch evidence, not a worktree or future gate pass. The six known affected package nodes are @fastify/busboy, braces, fast-glob, micromatch, openapi-typescript and undici; npm counts nodes, not distinct advisories (shared recon:119-120). Q9/Q10/Q11 proof and verification remain required; the specific known-advisory disposition does not waive other findings or gate requirements. The existing prohibition on --legacy-peer-deps and toolchain/contract workarounds remains intact.

**Next authorized step:** Quill makes the documentation-only T001 exact-pin correction, records the older-major/advisory/runtime-audit evidence and this ruling in plan.md:15, and adds npm audit --omit=dev to T021, preserving all three conditions. This recording leaves plan.md and tasks.md untouched. Their historical unresolved-version text and Phase 1 starts-immediately wording do not authorize implementation. Plan challenge/freeze, spec PR and Gate 1 remain separate required steps; this ruling clears the superseded pin/audit blocker only.

Process receipt: named worktree F:/Dev/LamuFlix.worktrees/feature-320-spec, branch feature/320-spec and HEAD 76a27a97e041546beb5e026f2c05a15540e1505f verified. Edited only brief.md and appended the full shared-note Patron ruling to CONCLUSIONS.md; preserved pre-existing changes. No temporary artifact read, independent recon, production edit, gate or commit.

## Plan-challenge Decisions A and B - current authority (2026-10-11)

Authorization: shared DEV-320:57-59 authorizes this documentation-only continuation in F:/Dev/LamuFlix.worktrees/feature-320-spec, branch feature/320-spec, HEAD dc90c081a2a010ce82b787ba2137ea4e2da12959. Prior entries above remain historical; the following Decision A supersedes the exact-29 condition and unversioned known-six wording at brief:121-123. Basis: Patron CONCLUSIONS.md:253-262; accepted Medium adjudications :242-245; shared DEV-320:54-55,58.

**Decision A - AMENDED condition 3 (Sentry F2):** Keep exact openapi-typescript@6.7.6 generator devDependency and openapi-fetch@0.9.8 runtime client. A plain install adds or changes only nodes in their dependency closure; no existing lockfile node changes version. No npm audit fix, overrides, --force or --legacy-peer-deps. The 29-package count is scratch-expected, not a real-lockfile invariant. T001 records the actual added count, package paths and pins-closure reconciliation from the real lockfile diff; T021 records fresh observed npm audit and npm audit --omit=dev commands, outputs and exits against the actual manifest/lockfile, with no pre-stated pass. Scratch six-node and omit-dev zero results remain evidence-limited observations, not future receipts. Basis: CONCLUSIONS:255-256; cited recon:115,133,141 and brief:123 evidence limits.

Known High findings are keyed by name@version, regardless of hoisted/nested path: @fastify/busboy@2.1.1, braces@3.0.3, fast-glob@3.3.3, micromatch@4.0.8, openapi-typescript@6.7.6 and undici@5.29.0. Any other High, any existing-node version change (including baseline undici@8.11.2), or non-zero omit-dev result returns to Patron with evidence; any deviation from pins-closure also returns to Patron. No ticket text or pins change, dependency swap or owner checkbox. No broader advisory clearance is inferred. Basis: CONCLUSIONS:255,257-258 and the cited Q11 evidence bar :142-143.

**Decision B - RATIFIED proof allocation (Compass F1):** T012 owns the Node strict unhandled-/api rejection proof in existing web/src/mocks/handlers.test.ts, explicitly after T011 establishes the sole setup.ts lifecycle. Use the exported server under that configured lifecycle to prove an unhandled /api request fails without reaching the real network. No new test file, msw.test.ts edit, duplicated lifecycle hooks or test logic in server.ts. Align US2 independent proof, SC-005 and Phase 2/3 proof references with T012; worker-policy/startup proof remains separately owned. This is an owner assignment within the approved requirement/file set. Basis: CONCLUSIONS:260-262; spec:43,85; brief:60,75; shared DEV-320:55,58.

**Sentry F1 correction ownership:** T002/T003 require only fixed committed web/src/api/openapi.json and no caller-supplied source, URL or glob; output-temp selection remains permitted. T013 owns input-source regression coverage in existing web/src/mocks/generate-api.test.ts alongside existing determinism/coverage/stale-temp proof. Basis: CONCLUSIONS:242; unchanged condition 1 at brief:121; findings-DEV-320-Sentry:7.

Correction boundary: one numbered documentation-only Quill list for Sentry F1/F2 and Compass F1, within brief:104 two-correction-round cap; no third repair loop, unresolved mismatch returns blocked to Conductor. Sentry F3 and Compass F2/F3 remain noted without repair (CONCLUSIONS:244,246-247). Read-back of corrected artifacts and any freeze disposition are separate later work. This recording authorizes no implementation, gates or commits and makes no freeze or Gate 1 claim; Rigger handles the separately authorized documentation commit. Prior content and unrelated changes preserved.
