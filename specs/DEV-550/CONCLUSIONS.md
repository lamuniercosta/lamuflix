# DEV-550 - Conclusions

## Q1 - Guard installation path (Phase 2 grill)
Ruling: yes - one shared guard via `ConfigureHttpClientDefaults`, set on the effective `SocketsHttpHandler.ConnectCallback`; ticket text, no ADR.
- Basis: ticket Wanted 1-2 name `SocketsHttpHandler.ConnectCallback` + `ConfigureHttpClientDefaults` on all named/typed clients (decided ticket text); recon-DEV-550 lines 10-12, 32-37 (5 typed registrations in 3 Infrastructure files, 1 `HttpClientHandler`, 0 constructed clients); PRODUCT.md 2.3 - no new dependency, project, layer, schema or API.
- Install point: one idempotent internal Infrastructure extension, invoked from each of the three existing client registration extensions, so every host that registers a client gets the guard without editing host `Program.cs` (stays inside the ticket directories).
- Handler rule: defaults configure the primary handler with the `(handler, sp)` configure overload, never by replacing it; the subtitle client switches to `SocketsHttpHandler` with `AllowAutoRedirect = false` via the same configure-not-replace path, so no per-client handler can drop the callback. OMDb quota, resilience and timeout registrations stay unchanged.
- Coverage: DI tests resolve each typed client's effective primary handler and assert `SocketsHttpHandler` with non-null `ConnectCallback` (plus `AllowAutoRedirect == false` for subtitles), in `tests/LamuFlix.IntegrationTests/`.
