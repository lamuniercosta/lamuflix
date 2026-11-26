---
status: accepted
---

# Browser stream audio conversion port

**Ticket** DEV-572 · **Accepted** in the Build PR that delivers the port and the Principle I amendment.

## Context

Movies whose selected audio track is AC3, E-AC3 or DTS cannot play with sound in the existing browser player. The API must prepare a completed, seekable MP4 in API-owned scratch (primary video stream-copied, selected audio converted to AAC stereo) and serve it through the authenticated `GET /api/movies/{id}/stream` with the existing 200/206/416 status set and byte-range semantics. Supported codecs, successful probes with no audio, and probe failures stay on the raw path.

Constitution Principle I requires external boundaries to be Core ports with Infrastructure adapters and an ADR. Process execution, probe/codec decisions, scratch lifetime and reader tracking must be testable without a web host, a real media library or a running browser. The LocalPlay `Infrastructure/Playback` folder owns player launch gated by `Features:LocalPlay` and must remain untouched.

## Decision

Add one narrow Core port, `LamuFlix.Core.Ports.IStreamConversionService`, with `PrepareAsync(int movieId, string sourcePath, CancellationToken)` returning a `StreamConversionResult` (resolved path plus whether conversion happened), and `OpenReadAsync(StreamConversionResult, CancellationToken)` returning a disposable `StreamReaderLease` for reader tracking. Supporting result and lease types are process-free and live beside the port.

Implement the adapter in `LamuFlix.Infrastructure/Streaming` — a new subfolder, not a new layer. It probes ffprobe JSON through the existing `IProcessRunner`, resolves ffprobe beside `ThumbnailOptions.FfmpegPath`, selects the first default-marked audio track or the first audio track in input order, converts only AC3/E-AC3/DTS, caches successful probes in memory by absolute path, size and last-write time, and warns then falls back to raw on probe failure. Cancellation propagates and is never logged as a probe failure.

One in-flight conversion per source version per API instance is shared by authorized waiters. A zero-waiter transition starts a 1 s internal reattachment grace; on expiry the child is killed and partial files are deleted. Conversion failure cleans up immediately. Completed output is published by atomic rename inside the same resolved scratch directory and survives Range aborts; it is governed only by idle expiry, size-bound LRU that skips active readers, source-version change, and owned orphan startup cleanup. Mutable lifetime state is instance-scoped. Options use the existing typed-options pattern with startup validation; relative scratch paths resolve under the content root in Infrastructure.

`SystemProcessRunner` and its registration stay in Thumbnails and are reused in place. Child arguments are structured `ArgumentList` values with shell execution disabled and contain only the server-resolved source path and generated scratch filenames. The endpoint authorizes and resolves the registered primary media through the existing library boundary before any child is invoked. `Features:LocalPlay` is neither read nor changed, no secret is added, and no dependency, schema, route, DTO, status code or protocol is added.

## Consequences

Principle I's closed port list gains exactly `IStreamConversionService`; the version changes 2.0.0 → 2.1.0 with Sync Impact Report and Last Amended, and `PortWhitelistPattern` gains the matching entry. These four changes ship atomically with the port definition so `LamuFlix.ArchitectureTests` stays green. Unit tests can drive probe decisions and conversion lifetime through fake runners and deterministic time without real media or processes.

The port deliberately carries no HTTP types: Content-Type, ranges, authorization and problem responses remain endpoint concerns. The single port does not split probe, cache and scratch into extra abstractions. A still-attached hung child remains bounded by the runner's existing API contract and Q4 termination rules; no feature-specific wall-clock conversion timeout is introduced.

## Rejected options

A general conversion service or a second port for probing/scratch would widen the approved envelope and weaken the closed-port rule. Moving the process runner out of Thumbnails or into LocalPlay `Infrastructure/Playback` would rewrite unnamed tracked files and entangle server-side conversion with local playback gating. Filename heuristics, persisted codec columns and provider-specific wrapper libraries are excluded by Q3/Q5 and PRODUCT §5.

Basis: DEV-572 grill Q1–Q5 and Step 6 F1/R1 in [CONCLUSIONS.md](../../specs/DEV-572/CONCLUSIONS.md), [brief.md](../../specs/DEV-572/brief.md), [spec.md](../../specs/DEV-572/spec.md), [plan.md](../../specs/DEV-572/plan.md), and the [constitution](../../.specify/memory/constitution.md). This ADR records the architecture only; proof, integrated acceptance and delivery gates remain separate.
