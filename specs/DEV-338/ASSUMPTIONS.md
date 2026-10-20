# DEV-338 taste assumptions

- [assumed] Q1: positive zero-weight identity fallback copy: `Identical feature profile`. Basis: Patron Q1 and ticket caller-safe Reason AC.
- [assumed] Q3: year decay k=0.1/year and runtime decay k=0.01/minute, selected by Patron within the ticket exponential formula.
- [assumed] Q4: labels Same director: <names>; Shared writers: <names>; Features <names>; Shares <genres>; Similar release years; Similar runtime; Similar rating. Generic empty-label fallbacks use the term meaning and no internal value.
- [assumed] Q9: empty sanitized shared-token fallbacks: Shared director; Shared writers; Shared cast; Shared genres. Omit individual empty sanitized tokens when other display tokens remain.
