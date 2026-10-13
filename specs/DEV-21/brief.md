# DEV-21 — Brief (Phase 2 step 3.1, grill close)

Ticket: DEV-21 — Create optimized multi-stage Dockerfiles for Api, Worker, and Web SPA
Size: M | ui: (none) | Parent: DEV-287
Worktree: F:\Dev\LamuFlix.worktrees\feature-021-spec | Branch: feature/021-spec
Grill base HEAD: 56afcf8fdc901a18b4235b7cc894e01d40db7150
Sources: YouTrack DEV-21 via scripts/get-task.ps1; specs/PRODUCT.md; specs/DEV-21/CONCLUSIONS.md (C1–C10, commit 56afcf8); specs/DEV-21/ASSUMPTIONS.md (2 items); notes DEV-21, recon-DEV-21 (sections 1–9.7), task-pipeline.

## Closing bar

- The grill is closed. No `blocked: structural` at grill close.
- This brief freezes scope, grill answers, and plan decisions for Quill. If it is not in this brief, it is not decided; a gap is `needs decision: <question>` to Keel, never a guess by Quill.
- Quill next drafts spec.md, plan.md, and tasks.md in one ask from this brief, recon-DEV-21, and specs/PRODUCT.md (Phase 2 step 3.2). Keel then runs /speckit-analyze read-only against this brief and sends one numbered fix list per round (step 3.3).
- Spec Kit is not started by this brief.

## Round caps

- Grill cap: 12 questions (task-pipeline section 2.2); beyond that, Keel recommendation tagged [assumed] in ASSUMPTIONS.md. Grill closed within cap; the 2 logged assumptions are in ASSUMPTIONS.md.
- Review cap: 2 rounds maximum; third-round findings go to user review with the PR.
- Remediation cap: 2 fix commits per round; a third means the finding or the plan was wrong.
- Blocked limit: 3 blocked reports parks the task.

## Owner answer amendment (Phase 3, PR #136)
- Owner answer on PR #136: option (c), other — drop `docker/Dockerfile.worker` permanently. This is not option (a) deferral to a follow-up after DEV-314/315/316, and it creates no follow-up Worker Dockerfile obligation from DEV-21.
- Basis: CONCLUSIONS C12 recorded `blocked: structural` under charter §2.3 escalation a because dropping `Dockerfile.worker` removes ticket Scope item 2 and part of SC-001; the owner has now answered that escalation on PR #136 (task note DEV-21, Phase 3 owner answer reconcile receipt). The ticket contradiction is therefore owner-decided, not silently assumed.
- Effect: this amendment supersedes every Worker-delivery clause below wherever it conflicts, including C1 and C2 as applied to `Dockerfile.worker`, C3, the C8 three-image acceptance wording, the Dockerfile.worker plan decision, and ordering constraints 2 and 4 as applied to Worker tasks. The Worker project itself is not deleted, rewritten, made buildable, or otherwise touched.
- Keel has amended brief.md, plan.md, and tasks.md for this decision change, and Quill has amended spec.md (US2, FR-002, and Key Entity cite C13 closing C12; SC-001/SC-002 cover Api and Web only; Worker evidence SC-004 is moot). No pre-answer Worker wording remains; nothing here restores Worker scope.

## Owner-answer resume decision (2026-10-13)

- Scope remains C13: Worker is dropped permanently; Api, Web, Nginx config and root `.dockerignore` are the only deliverables. Basis: PR #136 owner option (c) and test-plan option (c) checked as reported in DEV-21:452-454; this resumption is explicitly directed by Conductor.
- Preserve T001-T007 completion as historical work, not current-context acceptance. Recon-DEV-21 section 14 verifies the historical C8 pack at da3866cd3601b142fb3e9b3967b2ac4d3a4dea7b. Recon-DEV-21-3:46-73 proves the four delivered files and build manifests unchanged but records changed Api sources and `web/src/api/openapi.json` from main. Unchanged Dockerfile bytes do not prove builds with those changed inputs.
- Add **Phase 5: Owner-answer resume - current-context acceptance**, owned by Anvil: T009 repeats Api acceptance (T003), T010 repeats Web acceptance (T006), T011 collects the resulting current-context C8 evidence (T007). T009 and T010 follow the already-completed T001/T002/T004/T005; T011 follows both acceptance tasks. No implementation file rewrite is needed to start. A failing build stops with its command, exit and failure evidence; it does not authorize dependency, source, tag or scope changes.
- Preserve the historical pack and transcripts. T011 records new raw output paths separately under the existing `artifacts/DEV-21/` evidence location, plus branch, source HEAD, documentation working-tree delta, image IDs, command exits and per-image checks. This is evidence recording, not a new delivery folder or architectural layer. Each image is inspected/probed by its newly built ID, avoiding reuse of a historical tag as evidence.
- T008 remains Gauge-owned. Existing current-head gate receipts in DEV-21:442-450 are retained with their actual verdicts; the resume phase does not order Anvil to duplicate gates or mark T008 complete. After acceptance, Conductor routes any necessary gate refresh to Gauge for the resulting diff.
- Review and merge boundaries remain separate: recon-DEV-21-3:94-106 maps review coverage of the unchanged four files only. Spec amendments, feature-pin removal, rebased SHA coverage and main drift have no named current review receipt in those roots. This task amendment and new acceptance evidence do not establish a current-head merge bar, close review coverage, change the PR checkboxes, or authorize a merge.

## Frozen scope

In scope (implementation, Phase B):

- docker/Dockerfile.api — new file.
Four new files only; `docker/Dockerfile.worker` is excluded (Owner answer amendment above).
- docker/Dockerfile.web — new file.
- .dockerignore at repository root — new file (CONCLUSIONS C6).
- One new Nginx configuration file under docker/ for the Web image, serving the SPA and proxying /api to api:8080 (see Plan decisions). No existing file is rewritten to provide it.

In scope (this Phase A): specs/DEV-21/brief.md, then Quill-drafted spec.md, plan.md, tasks.md under specs/DEV-21/.

Out of scope (CONCLUSIONS C9; adding any of these is a ticket change):

- docker/Dockerfile.worker — dropped permanently by the owner answer on PR #136 (option c); no deferral follow-up is created by DEV-21, and the Worker project is not touched.
- docker-compose.yml and docker-compose.observability.yml.
- HEALTHCHECK instructions in any Dockerfile.
- CI workflow image builds; registry publishing.
- Any change to Directory.Packages.props, web/package.json, or any NuGet/npm dependency (C4).
- Any database schema change, public API route/DTO/status-code change, or contract-chain change (C10).
- Baking secrets, connection strings, or credentials into an image; setting Features:LocalPlay; adding anything that enables Process.Start playback (C7).
- Deleting or rewriting any tracked file the ticket does not name (recon section 3: no docker/, Dockerfile, or .dockerignore exists yet, tracked or untracked).

## Grill answers (every ruling, cited)

Decided by ticket text, not re-grilled:

- C1. docker/ top-level folder with Dockerfile.api, Dockerfile.worker, Dockerfile.web is approved. Basis: ticket Overview and Scope items 1–3; PRODUCT.md section 3. Charter care item 2 satisfied by ticket text. Superseded in part by the Owner answer amendment: Dockerfile.worker is dropped permanently; docker/ remains approved for Dockerfile.api and Dockerfile.web.
- C2. Base images and users are fixed: Api mcr.microsoft.com/dotnet/sdk:10.0 build to aspnet:10.0 runtime, USER app; Worker sdk:10.0 build to dotnet/runtime:10.0 runtime, non-root; Web Node build stage (npm run build) to Nginx alpine runtime, unprivileged, proxying /api to api:8080. Basis: ticket Scope items 1–3. Superseded in part by the Owner answer amendment: the Worker image clause no longer applies.

Patron rulings (CONCLUSIONS.md):

- C3. Worker runtime stays dotnet/runtime:10.0. Conditional trigger: if the plan shows the Worker needs Microsoft.AspNetCore.App (directly or via LamuFlix.ServiceDefaults), swapping to aspnet:10.0 changes ticket text; Keel reports it and Patron answers blocked: structural. Never swap silently. Verified in Plan decisions below. Moot after the Owner answer amendment and the C12 blocker: no Worker publish, runtimeconfig check, Dockerfile, or runtime image remains in DEV-21.
- C4. Dependencies: none. Base images are not NuGet/npm packages; Directory.Packages.props and web/package.json must not change.
- C5. Web runtime: an unprivileged Nginx alpine image (for example nginxinc/nginx-unprivileged:*-alpine) satisfies Scope item 3. Exact tag is picked in plan.md, pinned to a version, never latest.
- C6. Build context is the repository root. A new root .dockerignore is in scope; it is a new file, not a rewrite, forced by the acceptance criterion on minimal layer sizes.
- C7. Secrets and LocalPlay: no secrets, connection strings, or credentials in any image (runtime env/config only); no Dockerfile sets Features:LocalPlay or enables Process.Start playback.
- C8. Acceptance evidence: docker build succeeds for all three Dockerfiles from the repo root, and each final image runs as a non-root user (docker inspect / docker run id). Minimal layer sizes is qualitative: multi-stage, no SDK or Node in a runtime stage, restore layer cached before source copy, .dockerignore excludes build output, tests, and node_modules. No numeric size threshold. Superseded in part by the Owner answer amendment: acceptance now covers the Api and Web images only (two builds, each non-root).
- C9. Out of scope: compose files, HEALTHCHECK, CI image builds, registry publishing (noted, no ticket).
- C10. Schema (care item 3) and API shape (care item 4) are not touched. Contract chain OpenAPI to TypeScript is unaffected; the Web image builds the existing SPA as-is.

Assumptions (ASSUMPTIONS.md, taste, [assumed]):

- A1. minimal layer sizes is qualitative, with no numeric image-size threshold (= C8).
- A2. Web runtime is an unprivileged Nginx alpine variant with a pinned tag; exact tag left to plan (= C5).

## C3 verification (explicit, required by the ask)

Evidence (recon-DEV-21 sections 9.1–9.5, at HEAD 56afcf8):

- Worker project is src/LamuFlix.Worker/LamuFlix.Worker.csproj, project LamuFlix.Worker, Sdk Microsoft.NET.Sdk.Worker (csproj:1). PRODUCT.md line 10 says LamuFlix.Work; the tracked project is LamuFlix.Worker and no LamuFlix.Work.csproj exists (recon 9.1). Plan and spec use the tracked name.
- Worker has 0 FrameworkReference and 0 ProjectReference (recon 9.2, whole-file read). Its ProjectReference closure is exactly {LamuFlix.Worker} (recon 9.3).
- LamuFlix.ServiceDefaults declares Microsoft.AspNetCore.App at src/LamuFlix.ServiceDefaults/LamuFlix.ServiceDefaults.csproj:8, but no ProjectReference path from Worker reaches ServiceDefaults; within src/, AspNetCore.App enters only at ServiceDefaults and flows to Api and test projects, never to Worker (recon 9.3).
- Directory.Build.props and Directory.Packages.props add 0 FrameworkReference and 0 ProjectReference; no Directory.Build.targets exists (recon 9.4).
- Worker PackageReferences (csproj:10-14) contain no Microsoft.AspNetCore.* package, and Worker source has 0 aspnetcore matches and 0 ASP.NET Core API usage (recon 9.5).

Residual gaps (recon 9.6, not found in the named roots):

- Whether Microsoft.NET.Sdk.Worker implicitly contributes a FrameworkReference: not found in scope.
- Whether any NuGet package in the Worker graph declares a frameworkReference in its nuspec: not determinable in scope (no obj/project.assets.json exists; no restore was run).

Keel decision on the gaps: they do not change the C3 plan decision and do not require another recon round now. The project-reference evidence — the only path C3 names (directly or via ServiceDefaults) — is complete and negative, and the residual paths, if they existed, would surface in the published runtimeconfig, which recon cannot produce without a restore (a write recon must not do). The plan therefore:

- kept Worker runtime dotnet/runtime:10.0 with no blocked: structural at brief or plan stage (historical; superseded by C12 and the Owner answer amendment — no Worker runtime remains), and
- carried a mandatory build-time C3 check as an early Phase B task (historical; removed by the Owner answer amendment — T002 is deleted and no Worker publish or runtimeconfig inspection is performed).

No silent swap is permitted at any stage.

## Plan decisions

Approach:

- Every Dockerfile is multi-stage: build stage restores with only the project/manifest files copied first (cached restore layer), then copies source and publishes; runtime stage copies only publish/output assets. No SDK or Node in any runtime stage (C8).
- Build context for both builds is the repository root (C6), so restores see Directory.Packages.props, Directory.Build.props, and the referenced src/ projects.
- Dockerfile.api: sdk:10.0 build publishing src/LamuFlix.Api/LamuFlix.Api.csproj (recon 9.2: Api references Core, Infrastructure, ServiceDefaults), aspnet:10.0 runtime, USER app, entrypoint is the published Api assembly.
- Dockerfile.worker: dropped permanently by the owner answer on PR #136 (option c). No Worker Dockerfile, publish, runtimeconfig C3 check, or runtime image is planned; C3 is moot and the Worker project is untouched.
- Dockerfile.web: Node build stage running npm run build on the existing web/ SPA as-is (C10), then an unprivileged Nginx alpine runtime with a pinned tag picked in plan.md (C5). The Nginx config (new file under docker/) serves the SPA assets, falls back so client-side routes resolve to the SPA entry, and reverse-proxies /api to api:8080 (ticket Scope item 3). The plan names the exact build-output directory from the existing web build configuration; that is a plan-stage read of existing config, not a scope change.
- Exact pinned tags for the Node build image and the unprivileged Nginx image are chosen in plan.md, pinned to versions, never latest (C5 applied to both runtime and build images as sound practice). Keel checks the pins at /speckit-analyze.
- .dockerignore (root) excludes build output (bin/, obj/), tests, node_modules, and version-control/local artifacts so the root context stays minimal (C6, C8). Exact entries are drafted in plan/tasks; they must not exclude docker/, src/, web/ sources, Directory.Build.props, or Directory.Packages.props, which the builds need.
- No HEALTHCHECK, no compose file, no CI change, no registry step, in any task (C9).

Files touched:

- Spec phase (Phase A): specs/DEV-21/spec.md, specs/DEV-21/plan.md, specs/DEV-21/tasks.md (Quill, from this brief); this brief is the only Keel-written artifact. No ADR (M ticket; ADR is L-only, task-pipeline Phase 2 step 5). No design.md (no ui: tag).
- Build phase (Phase B, for plan/tasks to enumerate): docker/Dockerfile.api, docker/Dockerfile.web, docker/ Nginx config for the Web image, root .dockerignore. Four files, nothing else. Any further file is needs decision to Keel before it is added.

Test strategy:

- No existing test asserts on any Dockerfile (recon section 5: 0 Dockerfile hits in tests). No new unit/integration test project is created for this ticket, and no test file is rewritten.
- Acceptance is by C8 evidence as amended, executed in Phase B and recorded with raw command output: docker build -f docker/Dockerfile.api and docker build -f docker/Dockerfile.web, each from the repository root, both exit 0; for each final image, docker inspect / docker run id evidence that the user is non-root (uid not 0).
- Structural checks in the same evidence pass: runtime stages contain no SDK or Node; restore layer precedes source copy; .dockerignore covers bin/, obj/, tests, and node_modules; all image tags pinned (no latest).
- No C3 build-time check remains: the Worker line is dropped permanently, so there is no Worker acceptance or stop-and-escalate branch in the amended plan.
- Web proxy/SPA behaviour is evidenced by the Nginx config satisfying Scope item 3 on inspection (SPA fallback plus /api proxy to api:8080); running a compose stack to prove it live is out of scope (C9).

Gate expectations (Phase B, run by Gauge per task-pipeline Phase 3 step 5):

- This ticket changes no .cs, no production C# under src/, and no web/ source. Gates whose scope is therefore empty report exit 2 SKIPPED (scope-empty) verbatim: non-blocking, never PASS, never proof the code was checked. That includes the mutation gate SKIPPED (scope-empty) and web gates if /web is unchanged.
- Gates that do run must exit 0. Exit 1 or Could not run blocks; a property-test exit 2 blocks unless the task note records the opt-out line required by the pipeline.
- Never lower a threshold, never edit a generated file, and never substitute plain dotnet build for a gate script.
- docker build acceptance evidence (above) is this ticket's primary proof and is required in addition to the gate set, not instead of a gate that has real scope.

Task-ordering constraints (for Quill to encode in tasks.md):

1. Root .dockerignore is written before any docker build evidence is taken, because it defines the build context.
2. No Worker task remains: Dockerfile.worker, its publish/runtimeconfig C3 check, and its acceptance run are removed permanently, not deferred.
3. The Web Nginx config is written together with Dockerfile.web; neither is accepted without the other.
4. The Api and Web Dockerfiles are independent of each other once .dockerignore exists; the two docker build acceptance runs come after both Dockerfiles (and the Nginx config) are written.
5. No task may add a dependency, a compose file, a HEALTHCHECK, a CI change, or a tracked-file rewrite (frozen scope). Such a task is a brief violation for Keel to strike at /speckit-analyze.

## Next owner and action

- Current next owner: Anvil — **Phase 5: Owner-answer resume - current-context acceptance**, T009 then T010 then T011, under the resume decision above. T002 is already complete and is not restarted.
- Scope/task consistency is checked against this brief, plan DD-1/DD-6 and spec FR-001-FR-009/SC-001-SC-004; no Worker task or C3 build-time check remains.
- Gauge owns T008; Conductor routes later gates and outstanding review coverage separately. Nobody merges; the user merges every PR.

## Plan challenge adjudication (Phase 2 step 4, Keel)
Historical record: task IDs and Worker dispositions in this section use the pre-amendment numbering and scope. The Owner answer amendment above supersedes them; current numbering is tasks.md T001-T011 with no Worker task; T009-T011 are the owner-answer resume phase.

Read at HEAD 56afcf8fdc901a18b4235b7cc894e01d40db7150 on feature/021-spec: note DEV-21, findings-DEV-21-Ledger (F1-F6), findings-DEV-21-Compass (F1-F4), findings-DEV-21-Sentry (M1-M3, L1-L4), this brief, spec.md, plan.md, tasks.md, recon-DEV-21, specs/PRODUCT.md. Verified in-worktree: Directory.Build.props AdditionalFiles (CodeMetricsConfig.txt, BannedSymbols.txt via $(MSBuildThisFileDirectory), TreatWarningsAsErrors), global.json SDK 10.0.400 rollForward latestPatch. No finding contradicts ticket text (C1-C10) or departs from the constitution; no `blocked: structural` and no owner checkbox arises. The C3 Phase B stop-and-escalate trigger is unchanged. Quill applies the accepted text fixes below to spec.md, plan.md, and tasks.md only; Keel does not edit those files in this adjudication.

### Ledger (Standards)

- L-F1 Worker non-root mechanism: modified, accepted. Quill states in DD-3 that `USER app` is the built-in non-root user on dotnet/runtime:10.0 and that T006 non-root evidence is the fail-safe. No fallback is pre-authorised; if the image lacks that user or the build fails on it, Phase B stops and returns to Keel for a decision. Any created-user fallback stays inside the named Dockerfile.worker and is not a scope change, but it needs that Keel decision first. Owner: Keel; text: Quill; Phase B verify: Anvil.
- L-F2 Nginx listen port: accepted. Quill pins `listen 8080` and `EXPOSE 8080` in DD-4/T007 and adds the listen port to the T009 inspection list. Owner: Keel; text: Quill.
- L-F3 .dockerignore literal patterns: accepted, merged with S-M3 and S-L4. Quill enumerates literal patterns in DD-5/T001 (not categories) and T010 verifies nested `**/bin` and `**/obj` are actually excluded. Owner: Keel; text: Quill.
- L-F4 Independent Test wording: accepted, merged with C-F2. Quill adds one clarifying line that independence is file/runtime-level and ordering constraint 4 in tasks.md governs evidence order. Owner: Keel; text: Quill.
- L-F5 Parallel Example omits T005: accepted. Quill adds T005 (after T002) to the parallel example or states the dropping assumption. Owner: Keel; text: Quill.
- L-F6 T011 owner ambiguous: accepted, modified. T011 owner is Gauge (Phase 3 step 5 gate run per this brief Gate expectations); the implementer does not run a duplicate gate pass. Quill names Gauge as owner in T011. Owner: Gauge; text: Quill.

### Compass (Spec)

- C-F1 FR-008/FR-009 have no acceptance coverage: accepted, modified. Quill adds a negative structural check to each of T004, T006, and T009 (no HEALTHCHECK instruction; no secret-bearing ENV/COPY; no Features:LocalPlay or Process.Start enablement) and collects those results in T010, rather than adding the check to the Web tasks only. Owner: Keel; text: Quill.
- C-F2 Independent Test vs cross-story evidence dependency: accepted, merged with L-F4 (same clarifying line). Owner: Keel; text: Quill.
- C-F3 Brief amended after the 3.1 read-back receipt (138 lines recorded, 135 lines current, C3-verification section post-dating it): accepted as a record-only finding. This adjudication and the DEV-21 task note record the amendment for provenance; no content link is broken (Keel fix-list line references match current brief content) and no text fix is required. Owner: Keel.
- C-F4 /api vs location /api/ form: accepted, modified. Quill clarifies in FR-004/DD-4/SC-005/T007/T009 that the counted form is `location /api/` with prefix-preserving proxy to api:8080 for `/api/...` requests; a bare `/api` request without a trailing slash is not an acceptance requirement. This clarifies the ticket Scope item 3 proxy, already decided at C2; it is not a new public API shape (PRODUCT.md section 5 care item 4) and is not an owner escalation. No additional redirect or location block is added. Owner: Keel; text: Quill.

### Sentry (Risk)

- S-M1 FR-007 pin rule vs ticket-fixed .NET tags and global.json: accepted, modified. Quill reconciles FR-007/DD-1 so the ticket-fixed C2 .NET tags (sdk:10.0, aspnet:10.0, runtime:10.0) are exempt from the minor-pin wording; pinned means no `latest` and the ticket-fixed line for .NET images, with concrete pinned tags retained for Node and Nginx. Quill adds `global.json` to the copy-first set in DD-2/DD-3 (see S-M2) so the in-image SDK is held to the repo pin; if the sdk:10.0 image cannot satisfy global.json, Phase B stops and returns to Keel and Patron rather than changing any tag. Decision owner: Patron, on the basis of ticket Scope items 1-2 (C2) for the tag exemption; text: Quill; Phase B verify: Anvil. This is a spec-internal reconciliation, not a ticket change and not `blocked: structural`.
- S-M2 Build copy set omits CodeMetricsConfig.txt and BannedSymbols.txt: accepted. Verified at Directory.Build.props (AdditionalFiles via $(MSBuildThisFileDirectory), TreatWarningsAsErrors). Quill adds CodeMetricsConfig.txt, BannedSymbols.txt, and global.json (per S-M1) to the copy-first lists in FR-006, DD-2, DD-3, T003, and T005. Owner: Keel; text: Quill; Phase B verify at image publish: Anvil. This is the top Phase B verification item.
- S-M3 .dockerignore leaves non-source trees in context: accepted, merged with L-F3 and S-L4. Quill enumerates exact entries in DD-5/T001, excluding at minimum: `.git/`, `.agents/`, `.claude/`, `.codex/`, `.cursor/`, `.gemini/`, `.junie/`, `.specify/`, `.github/`, `docs/`, `specs/`, `scripts/`, `tests/`, `**/bin/`, `**/obj/`, `web/node_modules/`, `web/dist/`, plus editor/OS and local-artifact patterns from S-L4, while keeping `docker/`, `src/`, `web/` sources and the root build manifests (Directory.Build.props, Directory.Packages.props, CodeMetricsConfig.txt, BannedSymbols.txt, global.json, web/package.json, web/package-lock.json) visible. Owner: Keel; text: Quill.
- S-L1 Nginx listen port unspecified: accepted, duplicate of L-F2 (same DD-4/T007/T009 fix). Owner: Keel; text: Quill.
- S-L2 Proxy upstream, rewrite, and headers: accepted in part, rejected in part. Accepted: Quill specifies prefix-preserving `proxy_pass` (no URI part, so the `/api/` prefix is not stripped), `proxy_set_header` for Host, X-Real-IP, X-Forwarded-For, and X-Forwarded-Proto in DD-4/T007, and records that the Web image requires an `api` network alias at runtime. Rejected: a resolver or dynamic upstream and any live `docker run` or compose start smoke, because a live stack run is out of scope at C9 and adding it would change what the ticket delivers. Owner: Keel; text: Quill.
- S-L3 Api listen port and EXPOSE unstated: accepted, modified. Quill states `EXPOSE 8080` in DD-2 and records the image-default assumption (the aspnet:10.0 image sets ASPNETCORE_HTTP_PORTS=8080) in DD-2/T004; no ASPNETCORE_HTTP_PORTS environment value is set in the Dockerfile. Owner: Keel; text: Quill.
- S-L4 Dev appsettings and vague secret ignores: accepted, merged with L-F3 and S-M3. Quill adds explicit .dockerignore entries (`.env`, `.env.*`, `**/*.user`, `**/*.pfx`, `**/secrets.json`, `**/appsettings.Development.json`) to DD-5/T001 and adds a T010 confirmation that no development appsettings file lands in a runtime image. No literal secret was found by Sentry, and none is introduced. Owner: Keel; text: Quill.

### Escalation check and next owner

- No accepted finding adds a dependency, a project or folder beyond ticket-named docker/, a schema change, a public API shape, a secret or LocalPlay change, or a tracked-file rewrite beyond the frozen five-file set, so no section 2.3 care item needs a new Patron ruling beyond S-M1 (recorded above, Patron) and no finding meets escalation a (ticket contradiction) or b (constitution departure). Nothing is `blocked: structural`; Gate 1 has no new owner checkbox from this challenge.
- Next owner: Quill, to apply every accepted text fix above to spec.md, plan.md, and tasks.md only, with no new decisions. Then Keel re-runs /speckit-analyze read-only against this brief; a clean rerun freezes the plan for Gate 1 and the spec PR. Nobody merges.
