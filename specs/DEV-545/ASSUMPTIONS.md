# DEV-545 assumptions and presentation rulings

Status: grill closed after Patron Q1-Q9. No unverified factual value is adopted as a decision.

- [assumed] Diagnostic compare ties use ascending movie ID after descending score; plot ties use ascending ID after descending cosine. Patron approved Q5. This deterministic presentation order does not alter production ranking.
- [assumed] Quality bands are displayed between sorted unique adjacent bars using [lower,upper) boundaries; sample ties use IDs after the stable directed-pair hash. Patron approved Q7. Sparse samples display the actual available count, up to 12.
- [assumed] Empty denominators and missing ordinal medians display null/unavailable with counts rather than a numeric zero. Patron approved Q4/Q7. This wording makes missing observations explicit; the associated denominator rules are binding decisions in brief.md.

These presentation choices were deliberately ratified by Patron; none is a silent scope or architecture assumption. Ticket-defined diff labels and default bars remain authoritative ticket requirements.

The five real-library IDs, live connection string, owner judgments, current-main execution provenance, measurements and baseline publication are not assumed. The seed IDs will be looked up once during Phase B and populated in script defaults/help before acceptance, per Q2/Q9. Runtime credentials and personal outputs remain unversioned. The fixture About Time ID 90 must not substitute for a real library ID.

No structural ticket-delivery change or constitution departure was approved. If implementation requires one, use the prescribed owner checkbox; Patron approval cannot close it.

- [assumed] PC2 displays unavailable plot cosine/rank as null with the diagnostic pool limitation disclosed; PC3 ratifies exact SHA-256 directed sample ordering and UTF-8 compact output formatting. Basis: Patron PC2/PC3 in brief.md and CONCLUSIONS.md. These refine prior presentation rulings without inventing runtime data.
