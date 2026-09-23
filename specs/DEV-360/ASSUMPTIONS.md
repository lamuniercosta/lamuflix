# DEV-360 assumptions

- [assumed] The `KeyNotFoundException` message may use `"{typeof(T).Name} with id '{id}' was not found."`; the ticket requires only the entity type and id (`task-DEV-360:19`). This is message wording, not a new contract.
- [assumed] Phase B verification evidence (native exits, versions, the §3 Inconclusive text, the §5 probe `Up`, Ledger's `file:line` findings) is recorded in the PR body under a "Verification evidence" heading.

Owner decisions D1–D3 and Patron rulings are not assumptions; see [spec.md](spec.md) "Gate 1 and authorization envelope".
