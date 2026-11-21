# Research: DEV-539 TMDb Crowd similarity

**Date**: 2026-11-21  
**Authority**: `brief.md` Q1–Q8, `CONCLUSIONS.md`, ticket Open-point defaults, `recon-DEV-539`, ADR 0048.

## Decision: External catalogue item-to-item signal (ADR 0048)

- **Decision**: Use first-page TMDb `/movie/{id}/recommendations` as an external catalogue item-to-item similarity signal; amend ADR-0011; keep library-only candidates; keep local peer-history collaborative filtering prohibited.
- **Rationale**: Ticket Wanted 1 / AC 1; Patron Q1; metadata criteria miss “often liked together.”
- **Alternatives considered**: Local collaborative filtering (rejected by ADR-0011); TMDb similar lists / ratings / second sources (out of scope).

## Decision: Pair table including out-of-library targets

- **Decision**: Store unique `(source library movie id, recommended TMDb id)` pairs, including targets not yet in the library; resolve symmetrically at feature load.
- **Rationale**: Ticket Open points default; Patron Q2; late additions link without re-enrichment.
- **Alternatives considered**: Array-of-ids column only (rejected by ticket default); drop out-of-library ids (would force re-enrichment).

## Decision: Api operator backfill mode

- **Decision**: Explicit operator mode on existing `LamuFlix.Api` after configuration/DI and before web host; reuse typed client and resilience; atomic per-movie persist of id + rows.
- **Rationale**: Patron Q7; PRODUCT.md care list (no new project/route/dependency/`Process.Start`); ticket one-off command default.
- **Alternatives considered**: Separate console project; hosted background job; `Process.Start` wrapper; public HTTP endpoint (all rejected).

## Decision: Independent Crowd activation + evidence-gated default

- **Decision**: Crowd activation under `RecommendationOptions` independent of TMDb enrichment; checked-in default off unless leave-one-out top-10 hit rate ≥ baseline; retune strong-match bar from Crowd-active coverage/judged pairs when needed.
- **Rationale**: Patron Q5; ticket Wanted 7 / AC 10.
- **Alternatives considered**: Tie Crowd solely to `Tmdb:Enabled` (blocks enrichment-without-Crowd); enable by default without evidence (rejected).

## Decision: Non-fatal recommendations fetch on normal enrichment

- **Decision**: Carry TMDb id on successful enrichment; replace rows on successful recommendations fetch including empty; preserve on failure when id unchanged; clear stale rows when id changed; backfill stays stricter/atomic.
- **Rationale**: Patron Q6; matches keywords/credits non-fatal signal pattern in recon.
- **Alternatives considered**: Fail entire enrichment on recommendations errors (would drop metadata for a secondary signal); always clear on any failure (loses links on transient errors).
