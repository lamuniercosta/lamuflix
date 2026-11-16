# DEV-510 assumptions

## A1 [assumed] - Quantified headroom (amended after T002 home measurement, 2026-11-16)

Original Q4 assumption (`5x` = `725` liked movies `< 80%`) was extrapolated from the ticket's unmeasured `~1.3 KB/source` estimate before `T002` measured real home data (`EVIDENCE.md` Section 1c at `d1809a46`) and proved that retained `TasteProfile.Liked.Cast` (`~1.78 KB/source`; `800,238 B` across `449` movies) plus `LikedSources` retained maps (`~1.28 KB/source` mean) grow at `~3.06 KB/source` combined. Amended `[assumed]` A1 operationalizes ticket headroom on realistic retained maps (with `384`-float per-source embeddings in uncached inputs and zero reduction/thinning of source count, map content, or scoring data) as a two-tier budget against `IOptions<HybridCacheOptions>.Value.MaximumPayloadBytes` (`1,048,576 B` default):
1. **Ticket-baseline `< 80%` budget (`145` liked movies; `36.25x` the live `4`-source home profile)**: Serialized compact `CachedProfile` must be strictly below `80%` (`838,860 B`) of the effective ceiling (measured in `T002` at `465,340 B` = `44.4%` of ceiling, a `~59.4%` reduction from pre-change `~1.15 MB`).
2. **Multi-fold headroom `< 100%` ceiling + warning-band fixture (`300` liked movies; `2.07x` the `145`-source ticket baseline and `75x` the live `4`-source home profile)**: Serialized compact `CachedProfile` must be strictly below `100%` (`1,048,576 B`) of the effective ceiling (admitted and reused by `HybridCache` without `MaximumPayloadBytesExceeded`) while falling within the `[80%, 100%)` (`838,860 B`–`1,048,575 B`) near-limit warning band (measured in `T002` at `940,943 B` = `89.7%` of ceiling).

Basis: `recon-DEV-510` lines 58–66; `EVIDENCE.md` Section 1c (`d1809a46`); `CONCLUSIONS.md` Q4, PA1, and `T002 Feasibility Checkpoint Disposition`; Charter §2.3.

## A2 [assumed] - Warning default

Patron Q4 chooses configurable near-limit warning fraction 0.8. Cover below, equal-to, and above-boundary behavior, using the effective payload ceiling. Configuration has one source of truth; do not hardcode thresholds into production checks.

Basis: ticket work item 5, recon-DEV-510 line 64; CONCLUSIONS.md Q4.

## A3 [assumed] - Current-home payload headroom

Patron Q6 requires the current home profile to serialize strictly below the 80 percent budget once measured. T002 measured the live home profile at **4 liked sources** (not the ticket's 145 baseline): 41,908 B pre-change (4.0% of the 1,048,576 B ceiling; 5.0% of the 838,860 B strict-80% band) and approximately 23,090 B compact (~2.2%). The ticket's 145 liked movies is a fixture scale for the A1 ticket-baseline budget, not the current live-home source count. Actual bytes and library counts must be measured and recorded; estimates are not live measurements.

Basis: recon-DEV-510 lines 64-65; CONCLUSIONS.md Q4, Q6, and T002 Feasibility Checkpoint Disposition at 4abc7c53; EVIDENCE.md Sections 1 / 1c at d1809a46; ASSUMPTIONS.md A1 (amended).

At initial grill closure, no additional assumptions, taste rulings, or question-cap defaults were introduced. Seven questions were asked; all received explicit Patron rulings. No structural escalation or constitution departure was requested or approved.

## A4 [assumed] - Warning validation domain and diagnostic choice

Patron PA1 chooses finite 0 < WarningFraction <= 1 (default 0.8 remains A2), existing validation-message style and source-generated numeric logging only. Boundaries include NaN/infinities. Basis: ticket item 5, original recon line 64, targeted seams 3-7, CONCLUSIONS.md PA1. No silent revision of A1-A3: infeasible or unavailable budget proof stops for Patron; an owner checkbox is required only for ticket contradiction or constitution departure.
