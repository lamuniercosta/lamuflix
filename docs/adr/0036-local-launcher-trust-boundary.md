---
status: proposed
---

# Keep local player execution behind a proven local HTTP boundary

DEV-458 requires file-existence checks, an unmapped-player 422, enabled-LocalPlay startup refusal on non-loopback binds, and verification that shipped proxies cannot expose launch remotely. With `Features:LocalPlay=true`, every effective API listener must be proven loopback before any request is served, and `POST /api/movies/{id}/play` additionally requires a loopback raw peer and no forwarding-provenance headers. IPv4-mapped IPv6 peers/listeners are normalized before loopback checks; unknown peers/listeners fail closed. LocalPlay disabled imposes no new listener restriction.

A loopback proxy stripping provenance cannot be distinguished from a local client. Shipped nginx/Aspire paths must therefore exclude the play route from proxy forwarding or run with LocalPlay disabled; actual runtime smoke must prove they cannot elicit a launch. Such a stripping proxy exposing play with LocalPlay enabled is unsupported. We reject an absolute arbitrary-proxy claim and avoid introducing a token, mTLS or another execution channel beyond this ticket's existing HTTP design.

Within the existing Infrastructure launcher, reuse IFileSystem to check the selected file before player resolution and process start: missing file returns existing 404, unmapped format existing 422, and neither starts a process or appends playback history. Preserve DEV-438 selection, existing process-start semantics and one file argument. The check/start race remains a generic start failure (500, no retry/history), rather than pretending the filesystem and external process are atomic.

The cost is deliberate refusal of proxied launching and ambiguous bindings, plus required proof of an effective-listener hosting hook that fails before serving. That hook remains a pre-freeze recon requirement; this Proposed ADR does not claim implementation, Gate 1, runtime acceptance or owner approval. No new dependency, port, project, layer, database schema or secret is introduced.

Basis: ticket scope recorded in `recon-DEV-458:9`; PRODUCT sections 3/5; constitution Principles I/VII and API/contract rules; Patron's six exchanges and cited section 2.3 rulings in `specs/DEV-458/CONCLUSIONS.md`. Implementation footprint, review loop terms and mandatory nginx/Aspire verification are defined in `specs/DEV-458/brief.md`.
