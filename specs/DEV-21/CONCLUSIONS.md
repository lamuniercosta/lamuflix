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

## Unverified (outside the read scope of this ask)

- Exact Worker project name/path (`PRODUCT.md` says `LamuFlix.Work`) and its framework references: Keel to confirm from the csproj in plan (feeds C3).
- Constitution text beyond line 336 (cited via recon only).
