# Research: DEV-501 Explicit subtitle search and download

**Date**: 2026-11-12  
**Authority**: `brief.md` Q1–Q9 + D1–D5 + Principle V clarification, `CONCLUSIONS.md`, `ASSUMPTIONS.md`, ADR 0041, `recon-DEV-501` (incl. 111–130), `design.md`.  
No independent product decisions by Quill. Current-source inventories for prepare/shared files, installer, and some provider fields remain incomplete per recon:113–130 — implementation pickup verifies before mutation.

## Decision: Replace prepare with explicit search + download

- **Decision**: Remove prepare route/flow per D4 delete/update/retain table after current-checkout verification; add free GET search and POST download with viewer selection.
- **Rationale**: Owner rulings 5–7; prepare naming contradicts Subs BS Player layout; nothing calls prepare (recon:61).
- **Alternatives considered**: Keep prepare alongside search (rejected); auto-download on language change (rejected).

## Decision: Authoritative language catalog

- **Decision**: `SupportedSubtitleLanguages` + `GET /api/subtitles/languages` (`SubtitleLanguagesResponse` / `SubtitleLanguageOptionDto`); empty preferences valid; canonical trim/`OrdinalIgnoreCase` normalization; key-free private no-store.
- **Rationale**: Patron catalog ruling (Compass F1/F2); CONCLUSIONS.
- **Alternatives considered**: Hand-maintained web catalog (rejected); empty preferences as acquisition 409 (rejected).

## Decision: Provider seam, OpenSubtitles only

- **Decision**: Route acquisition through existing `ISubtitleProvider`; ship only OpenSubtitles adapter; keep public DTOs provider-neutral.
- **Rationale**: Owner ruling 6; ADR 0041.
- **Alternatives considered**: SubDL in this ticket (rejected); provider registry/SDK (rejected — new layer).

## Decision: Concrete Singleton binder + scorer (D2)

- **Decision**: Concrete `SubtitleCandidateBinder` (Singleton, no interface) and `SubtitleMatchScorer` in Core Features/Subtitles; typed TTL/cap options; 32-hex injected seeded-Random tokens without crypto claims; earliest-expiry FIFO eviction; process-local stale behavior.
- **Rationale**: Patron D2; AGENTS TimeProvider/seeded Random rules.
- **Alternatives considered**: `ISubtitleCandidateBinder` port (rejected — Principle I churn); DB token store (rejected); crypto RNG relabel (rejected).

## Decision: Quota / errors / Principle V (D1 + Cross-Artifact Alignment)

- **Decision**: Provider-observed nullable quota; Retry-After via `timeProvider.GetUtcNow().Add(duration)`; POST precheck order movie→token→read-only→config→containment; known-zero → download 429 before paid call; distinct search vs download 429; ProblemDetails reasons per D1; **422** for FluentValidation `ValidationException`; **400** for malformed HTTP/domain/token outside that exception; centralized `IExceptionHandler` + `AddProblemDetails` + `traceId`; no global remap; caller cancellation ≠ 504.
- **Rationale**: Patron D1 + Principle V clarification; constitution :551–569.
- **Alternatives considered**: All invalid inputs as 400 (superseded); remap ValidationException globally (rejected); invent failed-call billing (rejected).

## Decision: Streaming bounds and publication (D3)

- **Decision**: `MaxSubtitleDownloadBytes` default 2 MiB while streaming; `MaxSubtitlePublicationAttempts` default 10_000; reuse one staged leading-dot `.srt`; containment before create, after create/before stage, before each publication; cancel cleanup owned staging; attempt exhaustion → 409 `read_only_library`.
- **Rationale**: Patron D3/Q7/Q9.
- **Alternatives considered**: Buffer-then-measure body (rejected); unbounded collision loop (rejected); map cancel to timeout (rejected).

## Decision: Match percent formula

- **Decision**: Moviehash → 100; else invariant case-fold, Unicode letter/digit tokens, distinct-token Jaccard, `MidpointRounding.AwayFromZero`; empty union 0; sort match% ↓, downloadCount ↓, ordinal releaseName, candidateId. Meaningful property tests mandatory.
- **Rationale**: Ticket + Patron Q4/Q9; brief final drafting directions.
- **Alternatives considered**: Unspecified similarity (rejected); optional property proofs (rejected).

## Decision: Guarded Subs atomic publication (Q7)

- **Decision**: Validate SRT; stage leading-dot `.srt` inside movie `Subs\`; non-replacing publish to `<stem>-<code>.srt` / `(N)`; discovery excludes staging; cleanup owned staging only; never overwrite/delete published library files.
- **Rationale**: Ticket reuses DEV-453 atomic write while write guard forbids library deletes; Q7 supersedes Q5 direct-create; Q9 locks leading-dot staging + exclusion tests.
- **Alternatives considered**: Direct CreateNew without staging (rejected); delete-on-failure of competitor files (rejected).

## Decision: Read-only and SaveEnabled retention

- **Decision**: Retain `Subtitles:SaveEnabled`; GET search works when provider config valid with disabled downloadAvailability; POST 409 `read_only_library` before config 409 and before paid call.
- **Rationale**: recon:51–59; Q3/Q6/Q9; D1.
- **Alternatives considered**: Delete SaveEnabled as “prepare-only” (rejected).

## Decision: Constitution / ADR governance (D4)

- **Decision**: Post-Gate-1 Principle VII `1.15.0` → `1.16.0` with ADR 0041 acceptance together; ADR 0038 remains Proposed with prepare-route wording-only relationship; additive qualified `CONTEXT.md` glossary; no Principle I change; Principle V unchanged.
- **Rationale**: Patron Q2/D4; recon:122.
- **Alternatives considered**: Structural owner checkbox (rejected); accept ADR 0041 during planning freeze (rejected — keep Proposed until Gate 1).

## Decision: Active indicator (D5)

- **Decision**: Active check only on actually active downloaded track in LocalTrackList; candidates Get-only; no heuristic candidate↔track matching. Palette owns design.md wireframe/component repair.
- **Rationale**: Patron D5 / Q6 reconciliation with ticket flow.
- **Alternatives considered**: Candidate-row active checks (rejected).

## Decision: UI composition

- **Decision**: Extend Subtitles tab per Palette `design.md`; no new route or state library; media-chrome retained; consume generated catalog DTOs.
- **Rationale**: Q6; series ruling 1; catalog ruling.
- **Alternatives considered**: New top-level route (rejected).

## Open verification (not undecided product scope)

- Phase B: CC panel + Subs discovery dependency readiness and drift vs main.
- Current-source verification before prepare deletion, harness edit, installer/snapshot commands — recon:116/119/125/128 explicitly lacks complete inventories; D4 assigns verification to T003/T004/T006/T025/T026 before deletion/invocation. Missing facts stop as gated pickup `needs recon`, not product decisions.
- Runtime: home-cluster receipts OPEN / `Could not run` until executed.
- Plan freeze / Gate 1 / CLEAN analysis: owned by Keel/owner — not claimed by this research note.
