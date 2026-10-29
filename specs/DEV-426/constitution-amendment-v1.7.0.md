# Proposed Constitution amendment v1.7.0 (DEV-426)

**Status**: Proposed — staged into `.specify/memory/constitution.md` on `feature/426-spec` for this DEV-426 Phase A / spec PR review (Patron C1/C2). Owner Gate 1 checkbox in `spec.md` remains unchecked; **owner approval occurs through the Gate 1 spec-PR merge**. This is not present owner approval and is not a waiver.  
**Baseline (pre-staging)**: Constitution **Version 1.6.0** | Last Amended 2026-10-28.  
**Staged artifact**: Constitution **Version 1.7.0** with I/VII/VIII clauses below (Last Amended 2026-10-28 as staged).  
**Ticket**: DEV-426. **Basis**: `brief.md` post-grill C1/C2 correction and PC1/PC2 supplement; `CONCLUSIONS.md` C1/C2 and PC1/PC2 Patron rulings (PC2 supersedes I2 date-reconcile wording); Proposed ADR-0031 (`docs/adr/0031-omdb-request-gate.md`).  
**Precedents**: 1.4.0/ADR-0024; 1.5.0/ADR-0029; 1.6.0/ADR-0030.

This document is the Phase A written proposal record required by Governance (`.specify/memory/constitution.md`, Amendment procedure lines 597-602 and Compliance lines 609-612). **The amendment text is staged for spec PR review** with owner approval at Gate 1 merge — staging is not deferred to Phase B. After verified owner approval, Keel records the checkbox answer plus verified spec-PR merge receipt in `CONCLUSIONS.md` (T034). Phase B **verifies** the merged approved constitution is 1.7.0 with the staged I/VII/VIII clauses and Sync Impact Report with **no constitution edit at all, including no date edit** (T035). A date-only staged-vs-merge difference is **recorded in `CONCLUSIONS.md`**, not fixed in the constitution. Any substantive mismatch (version, Principle I/VII/VIII text, Sync Impact Report) or a non-matching baseline → stop `blocked: structural — ticket-named port/types vs constitution`. Keel changes ADR-0031 status to Accepted only in the **implementation PR** after verified owner approval and before code merge; no write to `main`. Owner rejection becomes the same blocked structural disposition. (PC2; PLAN_CHALLENGE Compass M3/L2, Ledger M2.)

## Purpose

One MINOR amendment covering both C1 and C2 while preserving ticket-named types and all unrelated substantive clauses:

1. **Principle I (C1)** — add Core port `IOmdbRequestGate` to the closed named-port enumeration; retain `Adding a port requires an ADR.` Companion: Proposed ADR-0031; architecture whitelist follow-up for `PortWhitelistPattern`.
2. **Principle VII (C2)** — add options record `OmdbQuotaOptions` to the options-record list.
3. **Principle VIII (C2)** — extend vendor-name exceptions with Core port `IOmdbRequestGate` and options record `OmdbQuotaOptions` only. New Infrastructure support types use functional names under existing VIII rules (PC1) — **this amendment does not widen VIII for Infrastructure support types**.
4. Bump Version **1.6.0 → 1.7.0** (MINOR), update Sync Impact Report and Last Amended as staged for review; review `.specify/templates/*` per Governance. Phase B does not reconcile or rewrite staged dates in the constitution file.

## Scope limits

- **Ticket names preserved**: `IOmdbRequestGate` and `OmdbQuotaOptions` exactly as named by ticket DEV-426 / note DEV-426:11. No rename workaround.
- **No unrelated constitution edits**: do not change other ports, options, vendor exceptions, stack rows, API/contract rules, debt items, or thresholds.
- **Architecture whitelist**: forced companion test edit in `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` (`PortWhitelistPattern` += `IOmdbRequestGate`), preserving existing tokens — not a constitution text change.
- **No waiver**: amendment path only; Gate 1 stays closed until owner approval; planning/analyze may continue against this proposal without claiming 1.7.0 is already accepted or authorising implementation.
- **No Phase B constitution mutation**: T035 is verify-only; date-only differences are recorded in `CONCLUSIONS.md` (PC2).

## Illustrative Sync Impact Report summary (proposal record only)

**Not an exact staged artifact and not a pending constitution edit.** The code block below is an **illustrative proposal summary** reflecting PC1/PC2 editorial intent for this proposal record. The authoritative Sync Impact Report for Gate 1 / T035 is the one already staged in `.specify/memory/constitution.md` (original staged date line and follow-up wording unchanged there). T035 compares the Gate-1-approved staged constitution (Version, Principle I/VII/VIII text, and **that** Sync Impact Report), not literal equality to this illustration. ADR delivery interpretation is governed by CONCLUSIONS PC1/PC2, not by the Follow-up lines in this illustration. Date-only staged-vs-merge differences are recorded in `CONCLUSIONS.md` only — no constitution mutation from this block.

```text
Version change: 1.6.0 → 1.7.0 (MINOR: Core port IOmdbRequestGate added to Principle I;
OmdbQuotaOptions added to Principle VII; IOmdbRequestGate and OmdbQuotaOptions added to
Principle VIII vendor-name exceptions; ADR-0031)

Amendment 1.7.0 (2026-10-28 staged; date-only merge variance recorded in CONCLUSIONS.md, not edited here):
  Modified sections:
    ✏️ I. Ports and Adapters: `IOmdbRequestGate` added to the closed named-port
       enumeration immediately after `IEnrichmentStatusNotifier` (DEV-426; ADR-0031).
       The BCL/M.E.AI abstractions and the sentence `Adding a port requires an ADR.`
       are retained.
    ✏️ VII. Configuration Isolation and Deterministic Time: `OmdbQuotaOptions` added to
       the options-record list beside `OmdbOptions` / `TmdbOptions`.
    ✏️ VIII. Ubiquitous Language in English: vendor-name exception adds the Core port
       `IOmdbRequestGate` and the options record `OmdbQuotaOptions` in
       `LamuFlix.Core.Options` (ticket-named; DEV-426; ADR-0031). Existing
       `TmdbOptions` and provider tag-value constant exceptions are retained.
       No wider Infrastructure support-type exception (PC1 functional names).
  Added / removed sections: none
  Templates reviewed:
    ✅ .specify/templates/*.md — review before Gate 1 merge; no port-enumeration, options-list,
       or vendor-exception references expected; record no-edit or required follow-up
  Follow-up:
    ✅ ADR-0031 Accepted by Keel in the implementation PR after verified owner Gate 1
       approval and before code merge (`docs/adr/0031-omdb-request-gate.md` status only).
    ✅ Update `PortWhitelistPattern` in
       `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs` to include
       `|IOmdbRequestGate` while preserving existing whitelist tokens.
```

## Principle I — closed port list (proposed replacement sentence)

Current (1.6.0, lines 192-196) ends the named ports at `IEnrichmentStatusNotifier` before the BCL/M.E.AI abstractions. Proposed text:

> Ports live in `Core/Ports/`; the only ports are `IMovieRepository`, `IMovieCatalog`,
> `IMetadataProvider`, `IEnrichmentQueue`, `IMediaLibraryScanner`, `IMediaPlayerLauncher`,
> `IRecommendationCandidateSource`, `IPlaybackHistory`, `IMovieFeaturesRepository`,
> `IEnrichmentStatusNotifier`, `IOmdbRequestGate`, and the BCL/M.E.AI abstractions
> `TimeProvider` and `IEmbeddingGenerator<string, Embedding<float>>`. Adding a port
> requires an ADR.

This amendment only **adds** `IOmdbRequestGate` after `IEnrichmentStatusNotifier`. Do not silently drop whitelist-only ports that may appear in `ArchitectureTests` but not yet in this sentence; reconcile unrelated divergence under a separate cited ruling.

## Principle VII — options-record list (proposed)

Current (1.6.0, lines 324-326) lists `LibraryOptions`, `PlaybackOptions`, `OmdbOptions`, `TmdbOptions`, `RabbitMqOptions`, `EnrichmentOptions`, `FeatureOptions`, `RecommendationOptions`. Proposed list (addition only):

> All configuration is bound to options records (`LibraryOptions`, `PlaybackOptions`,
> `OmdbOptions`, `OmdbQuotaOptions`, `TmdbOptions`, `RabbitMqOptions`, `EnrichmentOptions`,
> `FeatureOptions`, `RecommendationOptions`) with data-annotation validation and
> `ValidateOnStart()`. Reading `IConfiguration` by magic string outside options binding is
> forbidden.

## Principle VIII — vendor-name exceptions (proposed)

Current (1.6.0, lines 351-356) permits vendor names on the Infrastructure adapter, vendor-mandated configuration keys, `TmdbOptions`, and the `OmdbProviderName` / `TmdbProviderName` tag-value constants. Proposed exception sentence (additions only; retain unrelated clauses):

> Types, members, and telemetry identifiers MUST be named by functionality, not by vendor
> (`IMetadataProvider` and `Metadata.Lookup`, not `IOmdbClient` and `Omdb.Lookup`). Vendor
> names are permitted only on the Infrastructure adapter class itself, on vendor-mandated
> configuration keys, on the Core port `IOmdbRequestGate`, on the options records
> `OmdbQuotaOptions` and `TmdbOptions` in `LamuFlix.Core.Options`, and on the provider
> tag-value constants `OmdbProviderName` and `TmdbProviderName` in `TelemetryConstants`.

Infrastructure support types for this feature use PC1 functional names (`EfMetadataRequestGate`, `DailyRequestUsageRecord`, `DailyRequestUsageConfiguration`, `MetadataQuotaHandler`, `MetadataQuotaExhaustedException`, `MetadataQuotaPersistenceException`) and require **no additional VIII exception**.

## Footer (staged)

Staged: `**Version**: 1.7.0 | **Ratified**: 2026-09-21 | **Last Amended**: 2026-10-28`. Phase B does **not** edit this footer for merge-date variance; Keel records any date-only staged-vs-merge difference in `CONCLUSIONS.md` (PC2).

## Architecture whitelist (forced companion edit)

In `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs`, extend `PortWhitelistPattern` with `|IOmdbRequestGate` (exact token), preserving existing names, anchors, assertions, and anti-vacuity floor. No harness/threshold change. Cite Principle I Governance follow-up and ADR-0031.

## Template review expectations

Before Gate 1 merge, review `.specify/templates/*` per Governance step 4. Expect no references to the Principle I closed list, VII options list, or VIII vendor exceptions; record no-edit unless a template actually enumerates those clauses. Do not treat future review wording as complete evidence.

## Staging and verify constraints

- This amendment is **staged** into `.specify/memory/constitution.md` for spec PR review; owner approval is the Gate 1 spec-PR merge.
- Quill does not re-edit `.specify/memory/constitution.md` or ADR bodies.
- After Gate 1 owner approval: Keel records checkbox answer + verified spec-PR merge receipt in `CONCLUSIONS.md` (T034); Phase B **verifies only** (T035) that the merged approved constitution is 1.7.0 with the Gate-1-approved **staged** I/VII/VIII clauses and **that file’s** Sync Impact Report — compare the actual staged constitution, **not** literal equality to the illustrative Sync Impact Report summary above — **no constitution edit including dates**; date-only differences recorded in `CONCLUSIONS.md`; substantive mismatch vs staged constitution blocks.
- ADR delivery interpretation is governed by CONCLUSIONS PC1/PC2: Keel publishes ADR-0031 as Accepted (status-only) in the **implementation PR** after verified owner approval and before code merge; no write to `main`; not a same-delivery Phase 0 constitution mutation. The illustrative Follow-up wording above does not redefine that path.
- No production code that depends on the new port/options may merge ahead of owner-approved 1.7.0 and Accepted ADR-0031.
- Preparation and staging of this proposal (and ADR-0031 Proposed) are already done in the Phase A / spec PR for owner review; that work is not gated on Gate 1 merge and is not a Phase B drafting or apply task.
- Owner rejection → `blocked: structural — ticket-named port/types vs constitution`.
