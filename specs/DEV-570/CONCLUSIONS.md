# DEV-570 Conclusions

## Q1 — About Cast row, unmapped names
Verdict: Approved as recommended — mapped names open the existing MoreWith panel through the existing page selection path; unmapped names remain plain text, in comma-separated stored order, with a regression test covering both kinds of name.
- Ticket Scope (recon-DEV-570:52-55): Cast names comma-separated, as today a name selects the actor and opens the existing more with panel; hero cast section stays as is — ticket text is authoritative per specs/PRODUCT.md §3.
- Current behaviour (recon-DEV-570:24-26): CastSection routes matching names to onSelect buttons and leaves unmatched names as text; selection renders the existing MoreWithPanel. Reusing it adds no new API shape, layer, or dependency under specs/PRODUCT.md §5.
- No §2.3 escalation: the ruling follows the ticket rather than contradicting it, and departs from no constitution provision.
