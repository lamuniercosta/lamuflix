# DEV-550 - Conclusions

## Q1 - Guard installation path (Phase 2 grill)
Ruling: yes - one shared guard via `ConfigureHttpClientDefaults`, set on the effective `SocketsHttpHandler.ConnectCallback`; ticket text, no ADR.
- Basis: ticket Wanted 1-2 name `SocketsHttpHandler.ConnectCallback` + `ConfigureHttpClientDefaults` on all named/typed clients (decided ticket text); recon-DEV-550 lines 10-12, 32-37 (5 typed registrations in 3 Infrastructure files, 1 `HttpClientHandler`, 0 constructed clients); PRODUCT.md 2.3 - no new dependency, project, layer, schema or API.
- Install point: one idempotent internal Infrastructure extension, invoked from each of the three existing client registration extensions, so every host that registers a client gets the guard without editing host `Program.cs` (stays inside the ticket directories).
- Handler rule: defaults configure the primary handler with the `(handler, sp)` configure overload, never by replacing it; the subtitle client switches to `SocketsHttpHandler` with `AllowAutoRedirect = false` via the same configure-not-replace path, so no per-client handler can drop the callback. OMDb quota, resilience and timeout registrations stay unchanged.
- Coverage: DI tests resolve each typed client's effective primary handler and assert `SocketsHttpHandler` with non-null `ConnectCallback` (plus `AllowAutoRedirect == false` for subtitles), in `tests/LamuFlix.IntegrationTests/`.

- Question (Keel): Should the shared guard use `ConfigureHttpClientDefaults` in the existing Infrastructure registration path for each participating host, configure the effective handler, retain quota/resilience/timeout and the subtitle redirect setting, and stay in the ticket directories using built-in APIs with no ADR? Each host needs the shared registration, and the DI test needs the effective handler.
- Answer (Patron, delivered after commit `4fefc580`): Yes. Use an idempotent internal Infrastructure extension called from the three existing registration extensions, configure the effective handler in place, retain OMDb and redirect behavior, and assert the callback on each effective handler.


## Q2 - Dial-time address policy (Phase 2 grill)
Ruling: yes, Keel's policy, with these concrete sets; no constitution impact (hardening inside the ticket's own scope, no departure).
- Mandatory deny set, every client: 169.254.0.0/16 (link-local, includes 169.254.169.254 metadata), fe80::/10, 100.100.100.200 and fd00:ec2::254 (documented cloud-metadata endpoints), 0.0.0.0/8, ::/128, 224.0.0.0/4, ff00::/8, 255.255.255.255. IPv4-mapped IPv6 addresses are normalized with `MapToIPv4` before matching. OMDb/TMDb/Ollama keep configured loopback/private reachability.
- Stricter set, the whole OpenSubtitles client: mandatory set plus 127.0.0.0/8, ::1, 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16, 100.64.0.0/10, fc00::/7, 198.18.0.0/15. Test override is an explicit options flag, default false, that relaxes only this stricter set, never the mandatory one.
- Documentation: both sets live in one internal static range table with an XML doc citing the IANA IPv4/IPv6 Special-Purpose Address Registries (RFC 6890); the spec lists the same ranges. Basis: ticket Wanted 1 and Done when; recon-DEV-550 lines 10-12, 32-37; PRODUCT.md 2.3 (no dependency, schema or API change).
- Mechanics: resolve once in `ConnectCallback`, validate each candidate, connect to the exact validated `IPEndPoint`; no second resolution. Deny is a connect failure with a logged reason carrying no secrets or query strings.
