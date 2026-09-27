# DEV-295 ASSUMPTIONS

Taste rulings assumed by Patron (Q7). Every line is tagged `[assumed]` and is a value the
implementer copies verbatim; it consumes no grill question and no review round. Anything not
listed here is decided in `CONCLUSIONS.md` / `brief.md`.

## SafeDescription copy (Q7)

Patron accepted Keel's recommendation that the exact caller-safe copy is taste. The four strings
below are the frozen copy; they are provider-agnostic and carry no exception text, host, URL,
stack trace, or internal identifier (ticket Scope 2; constitution IV raw-text prohibition;
constitution VIII no vendor names in Core).

- `[assumed]` `provider_unavailable` -> `"The metadata provider is temporarily unavailable."`
- `[assumed]` `rate_limited` -> `"The metadata provider is temporarily rate limiting requests."`
- `[assumed]` `invalid_response` -> `"The metadata provider returned an unusable response."`
- `[assumed]` `unknown` -> `"The enrichment failed for an unknown reason."`

## Notes

- `Code` values are **not** taste: they are the stable lowercase snake_case contract values ruled
  in `CONCLUSIONS.md` Q7 (`provider_unavailable`, `rate_limited`, `invalid_response`, `unknown`).
- `IsRetryable` is ticket-decided, not taste: `ProviderUnavailable` true, `RateLimited` true,
  `InvalidResponse` false, `Unknown` true.
- No UI copy, sort order, or empty-state strings are introduced by this ticket.
