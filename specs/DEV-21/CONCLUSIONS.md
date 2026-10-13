# DEV-21 — Patron conclusions (Phase 2 grill)

Sources: YouTrack DEV-21 (Scope & Technical Design, Acceptance Criteria) via `scripts/get-task.ps1`;
`specs/PRODUCT.md`; charter §2.3; note `recon-DEV-21`.

Grill outcome: **no `blocked: structural`**. One conditional escalation trigger (C3) is handed to Keel to verify in plan.

## Decided by ticket text (cited, not re-grilled)

- C1. `docker/` top-level folder with `Dockerfile.api`, `Dockerfile.worker`, `Dockerfile.web` is approved.
  - Basis: ticket Overview (`in docker/`) and Scope items 1–3; PRODUCT.md §3. Care item 2 satisfied by ticket text.
- C2. Base images and users are fixed: Api `mcr.microsoft.com/dotnet/sdk:10.0` → `aspnet:10.0`, `USER app`;
  Worker `sdk:10.0` → `dotnet/runtime:10.0`, non-root; Web Node build stage (`npm run build`) → Nginx alpine, unprivileged, proxying `/api` to `api:8080`.
  - Basis: ticket Scope items 1–3.

## Patron rulings

- C3. Worker runtime image stays `dotnet/runtime:10.0`. **Conditional trigger:** if the plan shows the Worker project (directly or via `LamuFlix.ServiceDefaults`) needs the `Microsoft.AspNetCore.App` shared framework, swapping to `aspnet:10.0` would change ticket text: Keel reports it and Patron answers `blocked: structural`. Do not swap silently.
  - Basis: ticket Scope item 2; charter §2.3 escalation a.
- C4. Dependencies: none. Container base images are not NuGet/npm packages; no change to `Directory.Packages.props` or `web/package.json` is allowed by this ticket.
  - Basis: charter §2.3 item 1; recon §6.1 (0 docker lines in either manifest).
- C5. Web runtime image: an Nginx alpine image that runs unprivileged (e.g. `nginxinc/nginx-unprivileged:*-alpine`) satisfies Scope item 3; Keel picks the exact tag in plan, pinned to a version, not `latest`.
  - Basis: ticket Scope item 3; sound practice (reproducible builds).
- C6. Build context is the repository root (the Api/Worker builds need `Directory.Packages.props`, `Directory.Build.props` and the referenced `src/` projects). A new root `.dockerignore` is in scope: it is a new file, not a rewrite, and it is forced by the AC (`minimal layer sizes`).
  - Basis: ticket AC; charter §2.3 item 6 (forced edit; nothing tracked is rewritten — recon §3, 0 docker files exist).
- C7. Secrets and LocalPlay: images bake in no secrets, connection strings or credentials (supplied at runtime by env/config); no Dockerfile sets `Features__LocalPlay` or adds anything that enables `Process.Start` playback.
  - Basis: charter §2.3 item 5, scrub-list and LocalPlay hard rules.
- C8. Acceptance evidence: `docker build` succeeds for all three files from the repo root, and each final image runs as a non-root user (`docker inspect` / `docker run … id`). `minimal layer sizes` means: multi-stage with no SDK/Node in the runtime stage, restore layer cached before source copy, `.dockerignore` excludes build output, tests and `node_modules`. No numeric size threshold.
  - Basis: ticket AC; no size number in ticket text.
- C9. Out of scope (would add to what the ticket delivers): `docker-compose.yml`, `docker-compose.observability.yml`, `HEALTHCHECK` instructions, CI workflow image builds, registry publishing. `noted, no ticket` — constitution:336 lists compose as a target for Epic 8, which this ticket does not deliver; not Critical/High or broken behaviour.
  - Basis: ticket Scope items 1–3; charter §2.3 escalation a; PRODUCT.md §2 (Epic 8 by pick).
- C10. Care items 3 (schema) and 4 (API shape): not touched. Contract chain (OpenAPI → TS) unaffected; the Web image builds the existing SPA as-is.
  - Basis: recon §6.3–6.4.
- C11. S-M1 (ratified): the ticket-fixed .NET tags (`sdk:10.0`, `aspnet:10.0`, `runtime:10.0`) are exempt from the FR-007/DD-1 minor-pin wording; pinned means no `latest` plus the ticket-fixed line for .NET images, with concrete pinned tags for Node and Nginx. `global.json` joins the copy-first set; if `sdk:10.0` cannot satisfy it, Phase B stops and returns to Keel and Patron with no tag change. Spec-internal reconciliation, not a ticket change, not `blocked: structural`.
  - Basis: ticket Scope items 1–2 (C2); brief.md S-M1.

## Unverified (outside the read scope of this ask)

- Exact Worker project name/path (`PRODUCT.md` says `LamuFlix.Work`) and its framework references: Keel to confirm from the csproj in plan (feeds C3).
- Constitution text beyond line 336 (cited via recon only).

## Phase 3 rulings

- C12. T002 Worker publish blocker: **`blocked: structural`** (charter §2.3 escalation a). **Owner checkbox closed by C13: option (c).** The Worker line (T002, T005, T006) is parked; the Pomelo version-compatibility investigation is closed by owner instruction and is not reopened.
  - Basis: DEV-19 (commit 46b1d9b; specs/DEV-19 brief.md:137, spec.md FR-012) deliberately left `LamuFlix.Worker` unpinned, unbuilt and out of the solution for DEV-314/315/316, and deleted `LamuFlix.Data` (recon-DEV-21 §12.1). Worker source still compiles against that deleted project (`Program.cs:6,28` `using LamuFlix.Data` / `UseMySql(... MigrationsAssembly("LamuFlix.Data"))`, `EnrichmentJobProcessor.cs:12-13`, `Worker.cs:11`; recon §12.2). A Pomelo pin alone cannot make it publish, and C4 forbids touching `Directory.Packages.props` anyway, so the blocker is a retired project, not a package version.
  - Every way forward changes what the ticket delivers: making the Worker buildable is a rewrite of unnamed tracked files plus a dependency/provider decision (the DEV-314/315/316 work), and dropping or deferring `Dockerfile.worker` removes ticket Scope item 2 and the SC-001 "all three builds exit 0" criterion. Frozen scope (brief.md:23-40, :106; C4) authorizes neither. Owner checkbox on the DEV-21 delivery PR: **(a) defer `docker/Dockerfile.worker` and its SC-001/SC-002/SC-004 Worker evidence to a follow-up after DEV-314/315/316 rebuild the Worker (Patron recommends), or (b) expand DEV-21 to make the Worker buildable (rewrite off `LamuFlix.Data`, provider/dependency choice), or (c) other.**
  - Authorized now, within frozen scope: Api and Web writes T003, T007, T008 (independent of the Worker, tasks.md:83). Build-evidence runs T004/T009, T010 and T011 wait for the owner answer, because ordering constraint 4 (brief.md:128) ties them to all Dockerfiles being written; after the answer, Keel amends brief/plan/tasks and reruns `/speckit-analyze` before Anvil resumes.
- C13. Owner answer to C12: **option (c), drop `docker/Dockerfile.worker` permanently**; the C12 owner checkbox is closed and the `blocked: structural` escalation is resolved.
  - Basis: owner ticked (c) on delivery PR #136 (Rigger reconcile receipt, DEV-21 note:156-157); the owner's word is final (charter §2.3) and is not re-opened.
  - Dropped, not deferred: no deferral follow-up ticket is created, and `LamuFlix.Worker` stays untouched. DEV-21 delivers `Dockerfile.api`, `Dockerfile.web`, nginx config and `.dockerignore`; SC-001/SC-002 cover Api and Web only and Worker evidence (SC-004) is moot.
  - Next: Keel's brief/plan/tasks amendment (DEV-21 note:161) stands; Quill folds the spec.md fixes (note:163) and Keel reruns `/speckit-analyze` before Anvil resumes.
- C14 - .specify/feature.json repin to specs/DEV-21 (615f241) is gate-forced pipeline tool state per task-pipeline:62 and Patron rules §2.3(6) forced-edit clause; record-only, no action. Compass P4 F1 closed with receipt.
- C15 - PR #136 head alignment: **authorized** — Rigger may fast-forward `origin/feature/DEV-21` from 615f241 to the reviewed da3866cd (DEV-21 note:284-301; recon-DEV-21-2 §15).
  - Basis: 615f241 is the origin tip and an ancestor of da3866cd (§15.1); the four commits are the ticket's own deliverables (Dockerfile.api, Dockerfile.web, nginx.conf) plus C13, all already reviewed; pushing a feature branch is not a §2.3 care item, and why they were unpushed does not change their content.
  - Bounds: only `git push origin da3866cd3601b142fb3e9b3967b2ac4d3a4dea7b:refs/heads/feature/DEV-21` with no force flag, run only if HEAD = da3866cd, branch = feature/DEV-21 and the remote tip read back = 615f241; on any mismatch or non-fast-forward rejection, stop and report. Never stage, commit, stash, reset or discard the unstaged spec edits (incl. C14 and this C15). Read back PR #136 headRefOid = da3866cd afterwards.
  - Not authorized: the push itself until Bernstein sends it, artifact or pin change, re-review, PR comment or Phase 4 publication; the artifact and all findings are preserved. This line stays uncommitted until after alignment, since a commit now would move HEAD off the reviewed da3866cd.
- C16 - PR #136 Phase 4 publication: **authorized** — Rigger may post the existing nondeclined infrastructure-review findings and adjudication summary on PR #136 (DEV-21 note:266-310; charter:29).
  - Basis: PR #136 headRefOid now equals reviewed HEAD da3866cd (note:309), clearing the only blocker (note:285); the artifact (SHA256 3F92630B…E1D38D, note:281) and Keel closing bar (note:274-277) are unchanged; a review round is not finished until it is on the PR.
  - Bounds: one PR comment from the existing artifact only (Sentry L1 Low PLAUSIBLE as Follow-up, Ledger zero, Compass F1 Medium closed record-only under C14), naming reviewed HEAD da3866cd3601b142fb3e9b3967b2ac4d3a4dea7b and fixed point c79f33c6, and stating the C14 receipt is uncommitted working-tree state. Precondition: re-read headRefOid = da3866cd and artifact SHA256 match immediately before posting; on mismatch, stop and report. Read the posted comment (id and body) back into the DEV-21 note.
  - Not authorized: presenting the C# decline as infrastructure clearance or bundling it into the comment; artifact edits, re-review, commits, staging or discarding the five unstaged spec files. Stays uncommitted with C14/C15 until the post-publication spec commit (note:277).
- C17 - .specify/feature.json rebase conflict: **accept main 83a218e untracking; do not restore a tracked pin.** Resolved by Rigger receipt (DEV-21 note:402-404): `git rm --cached`, empty 615f241 skipped, remaining five picks continued without conflict; feature/DEV-21 = 9cd80c8 on main, PR #136 head 9cd80c8, ignored local pin at specs/DEV-21 present.
  - Basis: 83a218e is the owner's own commit (owner's word is final, overrides C14); task-pipeline:62 needs a worktree pin, not a tracked one, and the path is now gitignored (recon-DEV-21-2:78).
  - C14 stays closed record-only history, unrevised. Committed after the rebase, not mid-rebase, to avoid inserting a pick (note:400).
