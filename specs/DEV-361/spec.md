# Feature Specification: Restore five InspectCode warnings

**Feature Branch**: `feature/361-dev-361-spec`  
**Created**: 2026-09-23  
**Status**: Plan frozen (T013, 2026-09-23); gate1: provisional (Patron, 2026-09-23) — Gate 1 stays closed on the B1 owner checkbox and the owner merge  
**Input**: DEV-361 Scope & Technical Design (`task-DEV-361:14-23`) and acceptance criteria (`task-DEV-361:25-30`)

## User Scenarios & Testing

### User Story 1 — Five inspections block real defects (Priority: P1)

As a maintainer, I receive warning-level findings for the five downgraded inspections and can fix the concrete cases in their measured file set. **Independent test:** five `.editorconfig` values are `warning`; `run-jetbrains-inspectcode.ps1 -Files 'LamuFlix.Web/Models/Filmes/FilmesViewModel.cs','LamuFlix.Web/TagHelpers/Extensions.cs','LamuFlix.Test/UnitTest1.cs'` exits 0 after fixes and local suppressions.

### User Story 2 — External writers remain supported (Priority: P2)

As an MVC consumer, bindable properties remain available even when code search finds no assignment. **Independent test:** each remaining false positive is suppressed only at its type/member with a one-line external-writer reason; genuine dead members are fixed minimally.

### Edge Cases

- `TagHelpers/Extensions.cs:43,56` must compare `KeyValuePair` fields under the existing match guard; changing the latent null-value behavior is out of scope (Patron Q2).
- `UnitTest1.TestMethod1` and `TestMethod2` are ignored but compiled; DEV-280 is later, so simplify their four dead branches now and quote both names (Patron Q3; `chain:7-11`).
- **B1** is the owner's pending reversal checkbox for `resharper_nullable_warning_suppression_is_used_highlighting` (see Gate 1 below); its value and ruling are unchanged and the user's choice remains open. Gate 1 stays closed on B1 (T013 plan freeze is complete).
- `NullableWarningSuppressionIsUsed` remains at `suggestion` unless the user answers the B1 reversal checkbox. Its SUGGESTION probe hits do not make it one of the five authorized restorations.
- `JetBrains.Annotations` is not added. A finding that needs it is listed on the PR for Patron follow-up (`task-DEV-361:22`).

## Requirements

### Functional Requirements

- **FR-001**: Set these five `.editorconfig` keys to `warning`: `resharper_unused_auto_property_accessor_global_highlighting`, `resharper_collection_never_updated_global_highlighting`, `resharper_usage_of_default_struct_equality_highlighting`, `resharper_condition_is_always_true_or_false_highlighting`, and `resharper_condition_is_always_true_or_false_according_to_nullable_api_contract_highlighting` (`task-DEV-361:15-20,26`).
- **FR-002**: Restrict code fixes and AC2's InspectCode `-Files` target to the three paths measured by the Phase A probe: `LamuFlix.Web/Models/Filmes/FilmesViewModel.cs`, `LamuFlix.Web/TagHelpers/Extensions.cs`, and `LamuFlix.Test/UnitTest1.cs`. The probe found ten target diagnostic hits at nine lines in this set. A later whole-solution probe found one additional target-ID WARNING hit in `Views/Filmes/Details.cshtml:60`; record it for Patron follow-up without expanding this ticket (`research.md`; `CONCLUSIONS.md` Q1; `task-DEV-361:23,27`).
- **FR-003**: Resolve both `KeyValuePair` equality hits by comparing keys with `StringComparison.OrdinalIgnoreCase` and values with ordinal comparison under the existing guard, preserving reachable behavior. `SingleOrDefault` already throws if more than one case-insensitive key matches, so the guarded match has the same key identity despite default `KeyValuePair` equality being case-sensitive (`task-DEV-361:19`; Patron Q2).
- **FR-004**: Fix five diagnostic hits across four always-true branch sites (`UnitTest1.cs:61,72,101,112`; line 112 has both condition IDs) in the ignored `TestMethod1` and `TestMethod2` now; retain their intent and do not rely on future DEV-280 deletion (`research.md`; `task-DEV-361:20`; Patron Q3).
- **FR-005**: Classify binder/serializer findings by searching for in-product assignments. Suppress only proven external-writer false positives per type/member with a one-line reason naming the writer; fix real cases minimally. Every WARNING+ finding in the three-file AC2 target must be clean (`task-DEV-361:16-18,27-28`; Patron Q4).
- **FR-006**: Add no package or reference, including `JetBrains.Annotations`; route wider findings to Patron for a follow-up without scheduling chain work (`task-DEV-361:22,29`; charter §2.3).
- **FR-007**: Leave `resharper_nullable_warning_suppression_is_used_highlighting` at `suggestion` pending the user's explicit B1 reversal decision (`task-DEV-361:21,30`).

## Success Criteria

- **SC-001**: Exactly the five named inspection keys change from `suggestion` to `warning`; the B1 key stays `suggestion` until owner answer.
- **SC-002**: The warning-level InspectCode command with the frozen three-path `-Files` list exits 0.
- **SC-003**: Every remaining suppression is local with a one-line reason; no new package reference appears.
- **SC-004**: The five hits across four condition sites, two equality hits, and three view-model hits are resolved while MVC-bound properties remain available.

## Gate 1 and authorization envelope

- [ ] **Owner:** blocked: structural — restore `nullable_warning_suppression_is_used` to warning (reverses B1)? Gate 1 stays closed until the user answers (`task-DEV-361:21,30`; charter §2.3).
- [x] **Process:** Complete T013 plan challenge, adjudication, and plan freeze before the spec PR and Gate 1. Frozen 2026-09-23 (Round 1 of 2).
- [x] **Process (status record):** Patron set `gate1: provisional` on 2026-09-23. The spec PR is open for owner review only; the owner checkbox above stays unanswered, is not closed by any Patron ruling, and Phase B remains blocked on T002 until the user answers and merges.
- The five other `.editorconfig` keys and their bounded fixes are ticket-authorized. No public API, database schema, new dependency, or new project is introduced.
- Patron's DEV-361 ticket clarification/recon comment is prepared for Rigger; its YouTrack write remains unverified until read-back.
