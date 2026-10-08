# DEV-316 Grill Conclusions

## Q1 - Recovery eligibility and lease boundary

**Keel asked:** What exact recovery eligibility and lease-boundary contract should the sweeper use?
Recommended: `Status == Pending AND (LastAttemptAt is null OR LastAttemptAt < now - EnrichmentOptions.ClaimLease)`,
`now` from injected `TimeProvider`; re-enqueue through `IEnrichmentQueue` without claiming or changing
attempts/status; equal-boundary leases excluded; duplicate delivery tolerated via existing claim protection.

**Patron ruling: ACCEPT, amended** - predicate accepted as stated; re-enqueue goes through
`RequeueStrandedMoviesCommand`/`RequeueStrandedMoviesCommandHandler` (which depends only on `IEnrichmentQueue`), not a second direct enqueue path.

- Predicate is the constitution text verbatim: "re-enqueue `Pending` rows whose last attempt is null or older than the lease" (`.specify/memory/constitution.md:187-188`), and mirrors the worker claim predicate exactly, strict `<` included (`EfMovieRepository.TryClaimForEnrichmentAsync`, recon-DEV-316 line 41), so every row the sweeper selects is claimable and an equal-boundary row is neither swept nor claimable.
- No claim, no status/attempt mutation: owner-checked D1 on PR #54, "Drop sweeper claim, enqueue directly" (`docs/adr/0017-enrichment-decisions-in-core-handlers.md:69-83`); ADR-0004:94 names `RequeueStrandedMoviesCommandHandler` as the sweeper's enqueue path. Ticket "via IEnrichmentQueue" is satisfied because the handler's sole dependency is that port.
- Duplicate delivery is safe by the worker's atomic `ExecuteUpdateAsync` claim (recon line 41). Unbounded re-requeue / `MaxAttempts` across sweeps (ADR-0017:91-93) is NOT settled here; it is an open question for the grill.
