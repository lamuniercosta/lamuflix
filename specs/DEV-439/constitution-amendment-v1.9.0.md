# Proposed Constitution amendment v1.9.0 (DEV-439)

**Status**: Proposed — staged into `.specify/memory/constitution.md` on `feature/439-spec` for this DEV-439 Phase A / spec PR review (Patron Q1/Q6). Owner Gate 1 checkbox in `spec.md` remains unchecked; **owner approval occurs through the Gate 1 spec-PR merge**. This is not present owner approval and is not a waiver.  
**Baseline (pre-staging)**: Constitution **Version 1.8.0** | Last Amended 2026-10-30.  
**Staged artifact**: Constitution **Version 1.9.0** with Principle IV closed-list addition below (Last Amended 2026-11-01 as staged).  
**Ticket**: DEV-439. **Basis**: ticket Scope item 6 / AC 6; `brief.md` Q1/Q6; `CONCLUSIONS.md` Q1/Q6.  
**ADR**: none — Patron Q6 closed the grill with no new ADR (enumeration extension of an existing closed list, not a new architectural choice under Governance step 2).

This document is the Phase A written proposal record required by Governance (Amendment procedure). **The amendment text is staged for spec PR review** with owner approval at Gate 1 merge. After verified owner approval, Keel records the checkbox answer plus verified spec-PR merge receipt in `CONCLUSIONS.md` (tasks T021). Owner rejection becomes `blocked: structural` for DEV-333 Scope 3.

## Purpose

One MINOR amendment:

1. **Principle IV** — add `QuotaExhausted` to the closed `EnrichmentFailureCategory` enumeration list; align the cited retry parenthetical so `QuotaExhausted` shares the TTL retry-queue path with `RateLimited` until DEV-333 changes settlement.
2. Bump Version **1.8.0 → 1.9.0** (MINOR), update Sync Impact Report and Last Amended as staged for review; review `.specify/templates/*` per Governance (no template edit required).

## Scope limits

- **Ticket-named change only**: add `QuotaExhausted` to the Principle IV closed list cited at `.specify/memory/constitution.md:309-314`. Do not invent other list membership repairs in this amendment.
- **No new ADR**.
- **No unrelated constitution edits**: do not change ports, options, vendor exceptions, stack rows, API/contract rules, or thresholds.
- **No waiver**: amendment path only; Gate 1 stays closed until owner approval.

## Illustrative Sync Impact Report summary (proposal record only)

**Not an exact staged artifact and not a pending constitution edit beyond what is already staged.** The authoritative Sync Impact Report for Gate 1 is the one staged in `.specify/memory/constitution.md`.

```text
Version change: 1.8.0 → 1.9.0 (MINOR: Principle IV EnrichmentFailureCategory closed list
adds QuotaExhausted; DEV-439)

Amendment 1.9.0 (2026-11-01 staged):
  Modified sections:
    ✏️ IV. Enrichment: EnrichmentFailureCategory closed list adds QuotaExhausted beside
       ProviderUnavailable, RateLimited, InvalidResponse, Unknown; retry parenthetical
       notes QuotaExhausted shares the TTL retry-queue path with RateLimited until
       DEV-333. Classifier single-home rule retained.
  Added / removed sections: none
  Templates reviewed:
    ✅ .specify/templates/*.md — no closed EnrichmentFailureCategory list references;
       no edit needed
  Follow-up:
    ✅ Owner Gate 1 approval via spec-PR merge before Phase B implementation
    ✅ No ADR (Patron Q6)
```

## Principle IV — closed list (proposed replacement bullets)

Current (1.8.0) closed list omits `QuotaExhausted`. Proposed text for the cited bullets:

> - Failures are classified into the `EnrichmentFailureCategory` Enumeration (`ProviderUnavailable`,
>   `RateLimited`, `InvalidResponse`, `Unknown`, `QuotaExhausted`); each member carries its own
>   `IsRetryable` and caller-safe message. Classification from the root-cause exception lives in
>   exactly one `EnrichmentFailureClassifier`.
> - Retry is category-driven: retryable and attempts < max → republish with `Attempt + 1`
>   (`RateLimited` and `QuotaExhausted` via the TTL retry queue); otherwise `MarkFailed(category)`
>   and dead-letter.

This amendment only **adds** `QuotaExhausted` and aligns the retry parenthetical for the new retryable quota-admission category. Pre-existing membership differences versus code (for example `DuplicateImdbId`) are out of this ticket’s amendment scope.

## Staging / Gate 1

- Staged in `.specify/memory/constitution.md` on this branch for owner review.
- `spec.md` owner checkbox remains unchecked until Gate 1 merge approval.
- After approval: Keel records answer + merge receipt in `CONCLUSIONS.md` (T021). Phase B implements the category against the approved contract.
