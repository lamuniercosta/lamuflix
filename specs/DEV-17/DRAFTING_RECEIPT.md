# DEV-17 drafting receipt (Quill)

**Date**: 2026-10-30  
**Worktree**: `F:/Dev/LamuFlix.worktrees/feature-017-spec`  
**Branch**: `feature/017-spec`  
**Sources**: `brief.md` (incl. Phase 2 analyze + Phase 2 plan challenge round 1 binding corrections), `CONCLUSIONS.md`, `ASSUMPTIONS.md`, notes `recon-DEV-17` / `recon-DEV-17-2`, `specs/PRODUCT.md`, ADR 0033 (Keel-owned), constitution 1.8.0.

## Commands run

- Applied Keel Phase 2 plan challenge round 1 consolidated fix list (items 1–10) to Quill-owned artifacts only
- Applied Keel passage verification round 2 final bounded fix list (items 1–2) exclusively to `tasks.md` / `spec.md` / `contracts/thumbnail-generator.md` / `DRAFTING_RECEIPT.md`
- Did not edit `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, ADR 0033, `harness.yml`, production/test/governance code
- No commits, stage, or push

## Artifacts written / synchronized

Round 1 (full Quill set): `spec.md`, `plan.md`, `tasks.md`, `clarifications.md`, `checklists/requirements.md`, `research.md`, `data-model.md`, `contracts/thumbnail-generator.md`, `quickstart.md`, `DRAFTING_RECEIPT.md`.

Round 2 exclusive edits: `tasks.md`, `spec.md`, `contracts/thumbnail-generator.md`, `DRAFTING_RECEIPT.md` only.

## Phase 2 plan challenge round 1 — fix-to-file:line receipt

| # | Disposition | Primary anchors |
| --- | --- | --- |
| 1 | Applied — T010/T061 hosted ffmpeg+ffprobe proof blockers; no proven CI claim; no skip/filter/workflow; stop recon+Patron if absent; full suite/RequiresFfmpeg fail-if-missing retained | `tasks.md:37`, `tasks.md:108`, `plan.md:21`, `plan.md:108`, `quickstart.md:3`, `spec.md:72`, `spec.md:106`, `checklists/requirements.md:27` |
| 2 | Applied — injected TimeProvider + `CancellationTokenSource(timeout,timeProvider)`; DI/`System` fallback in T033; controlled timeout tests; tiny-byte fake-runner stays UnitTests; actual runner → IntegrationTests/Thumbnails | `spec.md:43`, `spec.md:100`, `tasks.md:66`, `tasks.md:68`, `tasks.md:70`, `contracts/thumbnail-generator.md:70`, `contracts/thumbnail-generator.md:94`, `plan.md` Structure IntegrationTests/Thumbnails |
| 3 | Applied — EvaluationCommand-style `pwsh -NoProfile -NonInteractive -File` fixtures under IntegrationTests/Fixtures/Thumbnails; narrow csproj copy; no xunit reentry/custom Main/new project/package; missing pwsh fails | `tasks.md:70`, `spec.md:104`, `contracts/thumbnail-generator.md:104`, `research.md:37`, `quickstart.md:25`, `clarifications.md:12` |
| 4 | Applied — closed ProcessResult + ThumbnailResult token sets with exact precedence/mapping; signatures preserved; no enum/factories; no raw diagnostics/paths | `spec.md:42`, `spec.md:100–101`, `contracts/thumbnail-generator.md:23–42`, `contracts/thumbnail-generator.md:75–83`, `data-model.md:13`, `data-model.md:43` |
| 5 | Applied — T034 `125.5`→`"12.55"` comma culture; exact discrete flags/scale (MaxWidth-substituted; `320` test example)/no upscale/spaced paths; SOI 0/1/reversed fail + `>=2` success; T035 one-stream 65536/65537 + simultaneous both-stream over-cap no-deadlock; Infra mutation N/A; Core unchanged; no mandatory FsCheck generator suite (synced after passage verification r2) | `tasks.md:69–70`, `spec.md:59–60`, `spec.md:85`, `spec.md:104`, `contracts/thumbnail-generator.md:88`, `data-model.md:43`, `data-model.md:51` |
| 6 | Applied — existing Hosting.Abstractions csproj:33; "present in Phase A working tree"; T002 diff/status + Rigger include constitution+ADR; harness.yml untouched/unstaged; no duplicate amendment/owner checkbox | `plan.md:15`, `plan.md:29`, `plan.md:100–103`, `tasks.md:25`, `research.md:25`, `research.md:43`, `spec.md:106` |
| 7 | Applied — T020 remove [P] + T020/T022 atomic; T024 term/definition/avoid check; T060 `-Help` + configured refactor threshold (no assumed `-Threshold`); missing source via ProbeFailed without File.Exists; T040 single RUN apt update/install -y --no-install-recommends/clear before USER app; T041 exact version outputs + size receipt | `tasks.md:49–51`, `tasks.md:53`, `tasks.md:83–84`, `tasks.md:107`, `spec.md:23`, `spec.md:42`, `spec.md:96–97`, `quickstart.md:29` |
| 8 | Applied — per-call timeout (≤2× + termination/cleanup); trusted operator FfmpegPath/CacheDirectory; SOI+real fixture preserved; Docker-before-fixture order preserved | `spec.md:44–45`, `spec.md:101`, `contracts/thumbnail-generator.md:26–27`, `plan.md:25`, `plan.md:110`, `tasks.md:92`, `tasks.md:120` |
| 9 | Applied — DEV-428 owns retention/eviction, consumer concurrency, library-root authorization (constraints only); ADR0031 duplicate + port mismatch already noted no ticket by Patron; no unrelated corrections | `plan.md:121–129`, `contracts/thumbnail-generator.md:110–114`, `spec.md:107`, `research.md:43`, `clarifications.md:15` |
| 10 | Applied — UTF-8 proven; no encoding rewrite. ADR 0033 editorial left to Keel (out of Quill ownership) | `research.md:43`, `research.md:45`, `spec.md:130`, `clarifications.md:20`, `checklists/requirements.md:40` |

## Passage verification round 2 — final bounded fix receipt

| # | Disposition | Exact repaired passages |
| --- | --- | --- |
| 1 | Applied — production scale substitutes configured MaxWidth; `320` labeled default/test example only (not fixed runtime width) | `contracts/thumbnail-generator.md:88`; `spec.md:60` (US3 scenario 2); `tasks.md:69` (T034); also mirrored in `spec.md:104` (FR-009 unit-test scale wording) |
| 2 | Applied — restored original Q7 simultaneous stdout+stderr over-cap complete-drain/no-deadlock/`OutputTruncated` alongside one-stream 65536/65537, ordinary success/capture, tree cleanup/cancellation, controlled clock | `tasks.md:70` (T035); `spec.md:85` (edge case); `spec.md:104` (FR-009); round-1 item 5 anchors synced above |

## needs decision

none

Quill did not edit `brief.md`, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, ADR 0033, or constitution. Assumed values remain only from `ASSUMPTIONS.md` / brief. No production code, tests, commits, push, stage, or gate/approval claims. Settled items not reopened.
