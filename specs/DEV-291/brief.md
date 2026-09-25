# DEV-291 Phase A brief

## Status

Phase A grill closed with Patron's rulings. The ticket authorizes NetArchTest.Rules; the selected version is 1.3.2. No structural owner checkbox is needed for this package. The scope, review closing bar, and two-round cap below are confirmed.

## Authority and frozen ticket scope

The DEV-291 ticket, Scope & Technical Design §1, explicitly names the four `net10.0` production projects: `src/LamuFlix.Core`, `src/LamuFlix.Infrastructure`, `src/LamuFlix.ServiceDefaults`, and `src/LamuFlix.Api`. The same section explicitly names `tests/LamuFlix.ArchitectureTests` as a `net10.0` test project. These five project additions are authorized by the ticket; they require no owner checkbox.

The ticket's §1 states Core depends only on BCL, `Microsoft.Extensions.Logging.Abstractions`, and `System.Collections.Immutable`, with no database or queue references; Infrastructure references Core; Api references Core, Infrastructure, and ServiceDefaults. ServiceDefaults covers shared hosting. The ticket's §2 requires architecture tests for Core's exclusion of EF Core, Npgsql, RabbitMQ, and Infrastructure; sealed Command and Query records; sealed Handlers; and cross-feature access through ports. Acceptance requires a compiling `LamuFlix.sln`, passing architecture tests, and failure when an EF Core reference is artificially added to Core.

The frozen review envelope is the five named projects and their files, `LamuFlix.sln`, `tests/LamuFlix.ArchitectureTests`, `specs/DEV-291`, and the single `Directory.Packages.props` package-version line. Anything else is a follow-up issue, not a finding in this round; Rigger records such follow-up tickets.

## Grill exchange and decisions

Keel asked Patron to confirm package handling and three review-loop terms. Patron ruled that NetArchTest.Rules 1.3.2 is the package choice, citing ticket Scope & Technical Design §1's named alternatives, the Summary's “NetArchTest rules,” and PRODUCT.md §3. Patron's rationale is that this is a ticket-authorized dependency and a specification decision, not a taste assumption. The Conductor confirmed Patron's authority to settle this named alternative. Record the choice and rationale in `spec.md`, not `ASSUMPTIONS.md`. If NetArchTest.Rules cannot express a ticket rule or fails on net10.0, switch to the other ticket-named option, ArchUnitNET, record the decision in `spec.md`, and report to Patron; do not add a checkbox. Patron also ruled that adding one `PackageVersion` line for the ticket-authorized package to `Directory.Packages.props` is an edit, not a §2.3.6 file rewrite.

Patron confirmed the frozen review envelope above. The review closing bar is Critical and High findings fixed and re-verified before PR; Medium and Low findings are recorded as follow-ups. The cap is two review rounds; after round two, escalate to the user rather than publishing a third round.

## Plan choices for Quill

Approach: draft the five named skeletons and architecture test project from the ticket boundaries. Use NetArchTest.Rules 1.3.2 for architecture tests and keep project references aligned to ticket §1. Architecture tests must exercise all four §2 rules and the acceptance counterexample that an EF Core reference in Core fails the architecture gate. Do not add speculative abstractions or production behavior beyond the skeletons.

Files in the planned envelope: the five named project directories and their project files, `LamuFlix.sln` (explicitly named by acceptance), `specs/DEV-291` artifacts, and one `PackageVersion` line for NetArchTest.Rules 1.3.2 in `Directory.Packages.props`. Any other existing-file rewrite is outside this envelope.

Test strategy: run the new architecture test project and full solution tests; verify the architecture rules fail for the ticket's artificial Core-to-EF reference in an isolated test setup. Exact implementation mechanics remain for the plan and tasks.

Gate expectations: solution build and architecture tests must pass. Run applicable harness analyzer and test gates on the implementation diff, reporting native exits. Recon reports no analyzer baseline because the planned files do not yet exist; no baseline is claimed here.

Task order: (1) Quill drafts spec, plan, and tasks from this brief; (2) challenge and freeze the plan; (3) add named skeletons and architecture tests; (4) run acceptance and applicable gates; (5) review within the confirmed closing bar and two-round cap.

## Taste assumptions and vocabulary

No taste assumptions were made; `ASSUMPTIONS.md` needs no entry. Ticket terms “Core,” “Infrastructure,” “ServiceDefaults,” “Api,” “ports,” “Command,” “Query,” and “Handler” are used as written. No new domain term or ADR was settled in the grill.
