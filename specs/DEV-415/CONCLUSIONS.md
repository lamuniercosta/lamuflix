# DEV-415 Grill Conclusions

## Q1 - Cache scope and cold-cache semantics

Worktree: `F:/Dev/LamuFlix.worktrees/feature-415-spec`; branch: `feature/415-spec`; source HEAD: `e010de28d26d492d5a93de9a90000118c9614f45`.

**Keel question and recommendation:** Confirm a single personal-library TasteProfile entry. A cold cache is a miss that computes from persisted inputs and yields empty only when no qualifying history exists, rather than returning empty merely because the cache is cold. Preserve existing computation semantics and cache the derived profile; never cache recommendation candidate filtering. A miss requires existing input reads; expiry must account for time-dependent calculation. The package is already authorized by ticket lines 17 and 20, so it requires no owner dependency checkbox. Official HybridCache API/package/expiry/tag/concurrency facts remain missing in recon line 55. Identify any interpretation that changes ticket delivery; do not invent API facts.

**Patron answer:** Confirmed: single personal-library TasteProfile entry; cold cache is a miss that computes from persisted inputs and yields empty only when no qualifying history exists. Ticket DEV-415 lines 17-23 scope the entry; lines 17 and 20 already authorize the package. No owner dependency checkbox and no change to ticket delivery. Recon-DEV-415 lines 19-22 and 44, together with PRODUCT.md section 3, require preserving existing computation semantics, caching only the derived profile, never recommendation candidate filtering, and next-read recomputation after playback. Recon line 55 supplies no official HybridCache API/package/expiry/tag/concurrency evidence. Do not invent those facts; expiry must account for the time-dependent calculation before API details are locked. Returning empty merely because the cache is cold would change ticket delivery and would be `blocked: structural` if proposed.

**Rationale and implications:** Repeated reads reuse the derived profile while candidate selection remains current. A miss uses persisted profile inputs. No package-version, expiry-value, concurrency, or invalidation-race design is settled by this ruling. Ticket-authorized Microsoft.Extensions.Caching.Hybrid does not need separate PRODUCT dependency approval (PRODUCT section 3; charter section 2.3 ticket-authority rule). Source: Patron Q1 response, 2026-10-23.

**Status:** Q1 settled. Grill remains open pending Wisp documentation recon. No brief or implementation authorization yet.
