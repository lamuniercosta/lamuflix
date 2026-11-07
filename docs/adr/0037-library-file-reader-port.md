# Authorize and open library files through a narrow Core port

Status: Proposed.

Ticket: DEV-456. Authority: [brief.md](../../specs/DEV-456/brief.md) Q7, Q10 and Q11;
full Patron rulings in [CONCLUSIONS.md](../../specs/DEV-456/CONCLUSIONS.md).
Date: 2026-11-07.

## Context

DEV-456 adds read-only primary-video HTTP streaming using the existing `IMediaLibraryScanner.ResolvePrimaryVideo` rule. Introduce `ILibraryFileReader` in `LamuFlix.Core.Ports` with `LibraryFileReader` in Infrastructure FileSystem to authorize the stored movie folder before resolution, then authorize and open the selected regular file once. Patron Q7 and Q10 choose this narrow filesystem boundary so real path/link authorization can be tested independently from HTTP result lifetime, without a general streaming service or a second selection rule.

## Decision

The API orchestrates the existing movie-details query, folder authorization, resolver, authorized open, MIME lookup, and framework file result. The port exposes `void AuthorizeFolder(LibraryPath folder)` and `Stream OpenRead(LibraryPath file)`; Stream is BCL, and HTTP/framework result types stay in the API. The adapter canonicalizes root and candidates, enforces separator-boundary containment using platform path rules, and rejects outside-root paths, sibling-prefix escapes, link/reparse components throughout the path chain, and non-regular files. Relative folders are normal; legacy rooted paths work only when contained. OpenRead authorizes the selected file and opens the authorized canonical full path once, using an asynchronous FileStream with FileShare.Read and Asynchronous | SequentialScan. The response uses the same opened handle, with ownership transferred to the file result and cleanup on pre-result failure.

`LibraryFileAccessDeniedException` in Core Ports denotes policy/safety denial, including non-regular paths; only that type maps route-locally to 403. Missing/disappeared folder or file uses existing NotFoundException (404). OS UnauthorizedAccessException and genuine I/O/permission/read failure remain sanitized 500; they are never blanket-translated into policy denial. This is the Q11 failure model, without an extra service or a second filesystem boundary.

## Consequences

One extra port buys separate real-disk containment tests and endpoint tests with narrow-port doubles. Reusing the launcher-bearing play handler would import LocalPlay gating and side effects; extracting thumbnail/cache/discovery containment or adding a general service would broaden the approved change. Streaming remains read-only, succeeds with LocalPlay disabled, and neither launches a process nor writes playback history. Legitimate linked paths fail closed. The trusted local library mount is the explicit threat boundary; an atomic sandbox against a malicious same-machine writer racing authorization and open is outside scope.

Principle I requires this ADR, a scoped named-port enumeration extension with the required constitution governance record, and architecture enforcement. In the Build PR, Keel verifies pickup governance then owns the explicit constitution edit: ILibraryFileReader only, appropriate MINOR bump (1.11.0 if the version remains 1.10.0), Sync Impact Report, Last Amended, template review and migration/follow-up record. Preserve existing entries and rules; DEV-467 tracks the prior omitted-port backfill separately. Complete this prerequisite before the port type slice. Add only `|ILibraryFileReader` immediately before `)$` in `PortWhitelistPattern` at `tests/LamuFlix.ArchitectureTests/ArchitectureTests.cs:30-31`, preserving all 16 existing entries, their order, and the test body; deliver the port and whitelist insertion together as one buildable slice before real-disk tests compile. Recheck these snapshot facts at Phase B pickup. Status remains Proposed until extension/governance exists and any applicable acceptance authority is evidenced; no automatic status transition or implementation compliance is claimed. Plan freeze, Gate 1, runtime acceptance, and merge clearance are separate.

Basis: [Q7 and Q10 rulings](../../specs/DEV-456/CONCLUSIONS.md), [brief](../../specs/DEV-456/brief.md), [constitution Principle I and Governance](../../.specify/memory/constitution.md); Maestri notes `recon-DEV-456` sections 2, 4-5, 7, 10 and `recon-DEV-456-3` section 3. Bernstein assigned ADR number 0037. Range, MIME, errors, generated contracts, and required Compose nginx/browser seeking remain governed by the feature brief; this ADR adds no new public contract.
