# DEV-542 grill conclusions

## Q1 — For You seed wording

**Keel asked.** Should the visible For You seed line use “Because you liked {title}” for each seed signal? Keel recommended the one template in the intake summary, with separate signal values in the data, and called out that this could imply a like for a watched or watchlisted seed. Keel asked Patron to flag any necessary delivery change for the owner.

**Patron ruled.** Use “Because you liked {title}” for `liked`, “Because you watched {title}” for `watched`, and “Because {title} is on your watchlist” for `watchlist`. Wanted item 2 expressly names all three templates. The earlier summary compressed that item. The exact ticket wording settles the decision under charter §2.3, without an owner checkbox.

**Basis and implications.** `recon-DEV-542:79–81,90–92` records the verbatim ticket text. Link each title to `/movies/{movieId}`, use “Open {title}” as the link accessible name, and cover all three signals in component tests.

---

## Q2 — Seed on the shared response DTO

**Keel asked.** Should `RecommendedMovieDto` describe `seed` as optional in OpenAPI, with Related omitting the JSON key and For You always sending a populated seed? Keel recommended that interpretation to preserve the shared generated DTO and meet the Related endpoint's evidence-only contract. Generated consumers will need to account for optionality, and wire tests can enforce the endpoint-specific guarantees.

**Patron ruled.** Yes. Keep the single shared DTO and omit the JSON key when the seed is null. A nullable seed property with a conditional JSON ignore attribute is one implementation. Related omits the key. Every For You item includes a populated seed.

**Basis and implications.** Wanted item 1, Done when item 3, `recon-DEV-542:52,89`, and `specs/PRODUCT.md` §5 item 4. Cover a populated For You seed and absence of the Related key in JSON integration tests, and assert the resulting generated API shape.

---

## Q3 — Evidence text and matched names

**Keel asked.** Which layer should format the three-names-plus-`+N` limit? Keel recommended that the backend supply display-ready text, preserve the complete ordered `matchedNames` list, and have the web display the server text verbatim. An API test would prove the formatting, and a component test would exercise a five-name item from that contract.

**Patron ruled.** The server owns the display-ready `text`. With five matched names it builds, for example, “Shared cast: Name One, Name Two, Name Three +2” and retains all five in `matchedNames`. Patron said that a component test can supply an item with five names and already-formatted text, and also discussed a possible client cap for fixtures that supply raw text. This plan uses the server-formatted response in the component test and renders `text` as given, so valid responses have one copy of the formatting logic.

**Basis and implications.** Wanted items 1 and 4, Open points, Done when item 5, and `recon-DEV-542:52,57,91`. Verify the formatted text and full name list at the server boundary and verify the visible first three names and “+2” in the card.

---

## Q4 — Dependency checkpoint at pickup

**Keel asked.** What rule applies if DEV-543 or DEV-518 cannot supply provenance at pickup? Keel recommended verifying both contracts before production edits and routing any gap to bounded recon and Patron. This lets Phase A plan against the ticket while protecting the existing result set and ranking.

**Patron ruled.** Require the checkpoint at Phase B pickup. Verify a real seed with movie ID, title, and one of the three supported signals for every For You item from DEV-543. Verify ordered criterion evidence from DEV-518 for For You and Related, including legitimate empty lists. A missing or insufficient contract stops pickup for bounded recon and a Patron ruling. Preserve recommendation scoring, ranking, and selection throughout.

**Basis and implications.** Ticket Needs, Technical design, Scope, and Out of scope at `recon-DEV-542:52–57,63,90`, charter §2.3, and `specs/PRODUCT.md` §§3 and 5. Phase A planning depends on the ticket contracts. Phase B must verify the upstream deliverables in its checkout.

---

## Q5 — Review closing agreement

**Keel asked.** Should the review close under a two-round cap, with all accepted in-scope findings from Critical through Low fixed or explicitly disposed with evidence, all applicable gates satisfied, and all acceptance evidence present? Keel proposed limiting the review to the seed/evidence contract and rendering, retirement of legacy reasons, generated artifacts, and ticket tests and evidence. Unresolved findings after round two would go to the PR.

**Patron ruled.** Confirmed the two-round cap and the closing bar at every severity. Require the home-library Hercules check and the before/after screenshots alongside the other acceptance evidence. The review scope is the DEV-542 response and rendering changes, legacy reason removal, generated OpenAPI and TypeScript artifacts, and the ticket tests.

**Basis and implications.** `recon-DEV-542:52–69,79–97`, the standing closing bar cited by Patron at `DEV-518:19–20`, and `task-pipeline` Phases 4 and 5. DEV-538 seed grouping, DEV-517 load more or swap, and scoring, ranking, or recommendation selection are outside the frozen scope. Anything else is a follow-up issue, not a finding in this round.

---

## Plan Challenge Ruling — Vocabulary, orphan cleanup, formatter seam, and upstream invariant guardrails

**Verdict.** Confirmed as proposed across all four plan-challenge decisions.

- **Vocabulary and formatter seam**: Keep domain vocabulary owned by DEV-543/DEV-518 with spec-local presentation terms in `specs/DEV-542/spec.md` and no root `CONTEXT.md` edit; reuse one pure Core DEV-518 formatter or existing feature-local seam located at pickup with no new architectural layer (Constitution VIII, Charter §2.3 item 2, `CONCLUSIONS.md` Q3–Q4, `recon-DEV-542:57`).
- **Legacy reason retirement and `Reason.cs`**: Retire `RecommendationReasonBuilder` and unused `ToReasonDto`/`Kind` mapping per ticket Technical Design, and delete `Reason.cs` only if Phase B pickup proves zero remaining callers (orphaned dead code); otherwise preserve unrelated callers intact (Charter §2.3 item 6 and dead-code rule, `recon-DEV-542:16–18,57`).
- **Upstream contract invariants**: Do not alter API status codes, invent fallback labels/seeds, or drop recommendations for malformed upstream seeds or unknown criteria; stop at the Phase B pickup checkpoint if upstream invariants cannot be demonstrated, and route any observed runtime defect back to Patron for disposition (Charter §2.3 item 4, `CONCLUSIONS.md` Q4, `recon-DEV-542:52–57,63`).

