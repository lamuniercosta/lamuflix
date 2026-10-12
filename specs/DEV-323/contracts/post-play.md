# POST /api/movies/{id}/play (DEV-323 unchanged)

- Route and 204/400/403/404/422 semantics unchanged; server-side LocalPlay adapter stays authoritative.
- Web mapping: 204 inline `Playback started.`; 403 accessible `Playback unavailable` modal; 404 inline removal message; 400/422/5xx/network inline generic error; button disabled only while in flight.
- No feature-flag endpoint; no backend, secrets, or legacy MVC change.
