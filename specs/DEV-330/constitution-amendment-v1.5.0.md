# Proposed Constitution amendment v1.5.0 (DEV-330)

**Status**: Proposed — owner Gate 1 checkbox in `spec.md` / `brief.md` remains unchecked.  
**Baseline**: Constitution **Version 1.4.0** | Last Amended 2026-10-22.  
**Ticket**: DEV-330. **Basis**: `brief.md` Q1; `CONCLUSIONS.md` Q1; ADR-0022; Proposed ADR-0029.

This document is the Phase A written proposal required by Governance (`constitution.md` amendment procedure). It does **not** amend `.specify/memory/constitution.md` until the user answers the owner checkbox and Phase B applies the change with the port delivery. If pickup finds a constitution version other than 1.4.0, stop for version reconciliation before editing.

## Purpose

1. Add ticket-authorized Core port `IEnrichmentStatusNotifier` to Principle I’s closed named-port enumeration.
2. Align enrichment-status delivery wording with accepted ADR-0022 (SignalR push supersedes polling for this channel).
3. Require the matching architecture whitelist token so `ArchitectureTests` stay consistent with the constitution.
4. Bump Version **1.4.0 → 1.5.0** (MINOR), update Sync Impact Report and Last Amended (actual amendment date at apply time).

Preserve all unrelated constitution rules and the existing port list; only the additions and enrichment-status wording named below change.

## Proposed Sync Impact Report entry (prepend when applying)

```text
Version change: 1.4.0 → 1.5.0 (MINOR — Core port enumeration expanded with
`IEnrichmentStatusNotifier`; enrichment status delivery wording aligned to ADR-0022)

Amendment 1.5.0 (<apply-date>):
  Modified sections:
    ✏️ I. Ports and Adapters: `IEnrichmentStatusNotifier` added to the closed named-port
       enumeration immediately after `IMovieFeaturesRepository` (DEV-330; ADR-0029).
       The BCL/M.E.AI abstractions and the sentence `Adding a port requires an ADR.`
       are retained.
    ✏️ API and Contract Rules: enrichment status delivery sentence updated from
       polling-only P1 / SignalR stretch (ADR-0008) to SignalR push per ADR-0022
       (polling superseded for this channel only).
    ✏️ Known Technical Debt: remove or reword the standalone “Polling for enrichment
       status instead of push (ADR-0008)” debt item so it no longer contradicts
       ADR-0022 / ADR-0029 (supersession already recorded on ADR-0008).
  Added / removed sections: none
  Templates reviewed:
    ✅ .specify/templates/*.md — review at apply time; no port-enumeration references
       expected; record no-edit or required follow-up
  Follow-up: update `PortWhitelistPattern` in
    `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` to include
    `|IEnrichmentStatusNotifier` while preserving existing whitelist tokens
```

## Principle I — closed port list (proposed replacement sentence)

Current (1.4.0) ends the named ports at `IMovieFeaturesRepository` before the BCL/M.E.AI abstractions. Proposed text (additions in bold intent; apply as plain text):

> Ports live in `Core/Ports/`; the only ports are `IMovieRepository`, `IMovieCatalog`,
> `IMetadataProvider`, `IEnrichmentQueue`, `IMediaLibraryScanner`, `IMediaPlayerLauncher`,
> `IRecommendationCandidateSource`, `IPlaybackHistory`, `IMovieFeaturesRepository`,
> `IEnrichmentStatusNotifier`, and the BCL/M.E.AI abstractions `TimeProvider` and
> `IEmbeddingGenerator<string, Embedding<float>>`. Adding a port requires an ADR.

Do not remove other ports that may already appear in the architecture whitelist but not yet in this sentence; this amendment only **adds** `IEnrichmentStatusNotifier` after `IMovieFeaturesRepository`. If Phase B recon finds the constitution sentence and whitelist already diverge for unrelated ports, reconcile under a separate cited ruling — do not silently drop whitelist tokens.

## API and Contract Rules — enrichment status delivery (proposed)

Replace the polling-only sentence (currently at the enrichment-status bullet citing ADR-0008 / SignalR stretch) with wording that matches ADR-0022:

> Enrichment status is delivered by SignalR push from the Api host (ADR-0022; port and
> post-commit semantics in ADR-0029). Client reconnect refetches pending movies rather
> than replaying missed events. Polling (ADR-0008) is superseded for this channel only.

## Known Technical Debt — polling item (proposed)

Remove or rewrite the debt bullet that still states polling instead of push (ADR-0008) so it does not contradict the Accepted channel decision. Prefer deletion of that debt item if ADR-0008’s supersession pointer and the API/Contract Rules sentence already record history; do not rewrite ADR-0008 history.

## Footer (when applying)

Update: `**Version**: 1.5.0 | **Ratified**: 2026-09-21 | **Last Amended**: <actual apply date>`

## Architecture whitelist (forced companion edit)

In `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`, extend `PortWhitelistPattern` with `|IEnrichmentStatusNotifier` (exact token), preserving existing names, anchors, assertions, and anti-vacuity floor. No harness/threshold change.

## Apply constraints

- Same build PR as Accepted ADR-0029 publication and the port implementation (constitution compliance rule).
- No production code that depends on the new port may merge ahead of this amendment and Accepted ADR-0029.
- Quill does not apply this file to `.specify/memory/constitution.md` in Phase A; Phase B does after Gate 1.
