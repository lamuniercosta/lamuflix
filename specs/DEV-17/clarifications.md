# Clarifications — DEV-17 (speckit-clarify, 2026-10-30)

Source of truth: `CONCLUSIONS.md` Q1–Q10 (Q3 supersedes Q1), Phase 2 analyze, Phase 2 plan challenge round 1, and `brief.md`. This pass re-checked the drafted `spec.md` against those rulings. No new decision was made here.

| # | Question | Answer (cited basis) |
| --- | --- | --- |
| C1 | Abstraction placement | Core `IThumbnailGenerator` + process-free `ThumbnailResult`; Infrastructure Thumbnails owns runner/adapter — Q3 (supersedes Q1); ticket recon:84–85. |
| C2 | Process.Start / LocalPlay | Dedicated ArgumentList runner with injected `TimeProvider`; independent of LocalPlay; playback starter untouched; cancellation vs failed-result split — Q2 + plan challenge r1 item 2. |
| C3 | Generator/result/output ownership | Exact `GenerateAsync`/`ThumbnailResult` shape; generator-owned cache JPEG; Disabled short-circuit; Length ≥ 2 JPEG SOI validation; closed FailureReason catalogue — Q4 + plan challenge r1 item 4. |
| C4 | Options defaults and validation | Enabled=false ticket; assumed path/width/timeout/section; conditional startup validation without binary probe; ffprobe sibling/PATH; content-root via existing Hosting.Abstractions csproj:33; trusted operator FfmpegPath/CacheDirectory — Q5 + ASSUMPTIONS + plan challenge r1 items 6/7. |
| C5 | Runner and extraction semantics | Exact `RunAsync`/`ProcessResult`; closed ProcessResult tokens; 64 KiB cap; per-call timeout (≤ 2× + cleanup); 10% fractional seek; discrete ffmpeg flags/scale; missing source → ProbeFailed without File.Exists — Q6 + plan challenge r1 items 4/5/7/8. |
| C6 | Test and Docker proof | Fake-runner UnitTests + IntegrationTests `pwsh` fixtures + RequiresFfmpeg + app-user exact version/size receipt; hosted tools unproved (T010/T061 blocker); missing tools/`pwsh` fail; no CI edits; SOI + Docker-before-fixture preserved — Q7/Q9 + plan challenge r1 items 1/3/8. |
| C7 | File envelope and ADR number | Eight new production files + narrow wiring; `IProcessRunner` co-located; ADR 0033 + constitution 1.8.0 present in Phase A working tree (Rigger includes after diff/status; harness.yml unstaged); no duplicate amendment/owner checkbox — Q8 + plan challenge r1 item 6. |
| C8 | Ordering and gates | Phase A docs/analyze/challenge/Gate 1; Phase B drift + hosted-tool proof then port/options before adapter, Dockerfile (T040) before real-fixture; T020+T022 atomic; T060 `-Help`-documented refactor invocation; L-tier gates + Core mutation; Infrastructure N/A — Q9 + plan challenge r1 items 1/5/7. |
| C9 | Closing bar and frozen scope | Zero unresolved C/H/M; Low dispositioned; two-round cap; DEV-428 owns retention/concurrency/library-root auth (constraints only); DEV-429 and listed exclusions out of scope — Q10 + plan challenge r1 item 9. |
| C10 | Charter 2.3 | No owner structural checkbox; ticket-approved Core port and Process.Start rulings; IntegrationTests fixture-copy + pwsh technique authorized; ordinary owner Gate 1 remains — brief Charter table + plan challenge r1 item 3. |
| C11 | Gherkin | Acceptance-tests stage remains opt-out; unit + integration required — pipeline policy + brief (no Gherkin request). |
| C12 | PRODUCT.md authority | Ticket Scope & Technical Design is authoritative; cite ticket lines and move on — PRODUCT.md §3; recon:80–117. |
| C13 | Phase 2 analyze corrections | Co-locate `IProcessRunner`; authorize `CONTEXT.md` Thumbnail reconcile/add (+ optional VIII summary); Principle VI health/telemetry N/A for local in-image CLI — brief Phase 2 corrections 1–3; CONCLUSIONS Phase 2 analyze. |
| C14 | Phase 2 plan challenge round 1 | Hosted-tool proof blocker; TimeProvider + IntegrationTests placement; pwsh fixtures; closed tokens; strengthened T034/T035 assertions; Hosting.Abstractions present; task/contract clarifications; SOI + Docker-before-fixture; DEV-428 boundaries; UTF-8 proven (no rewrite); ADR editorial owned by Keel — CONCLUSIONS Phase 2 plan challenge round 1; brief binding corrections. |

Open `needs decision` items for Keel: none. No gap was found that the brief and conclusions do not already decide; any future gap becomes `needs decision: <question>` and is never filled by Quill.
