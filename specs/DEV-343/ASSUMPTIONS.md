# DEV-343 Taste assumptions

## Q3 [assumed]
Labelled carousel region and semantic list; viewport ArrowLeft/ArrowRight only on self-focus; Previous/Next controls disabled at boundaries and when content fits; native Tab/Enter and modified links; no wrap, autoplay or roving focus; visible focus and reduced-motion support. Patron approved Q3; full basis and exchange in CONCLUSIONS.md.

## Q4 [assumed]
Section: Related movies. Loading: Loading related movies… Empty: No related movies yet. Error: Could not load related movies. Action: Retry. API order is retained. Full ruling in CONCLUSIONS.md.

## Q5 [assumed]
Keep API Reason ordering; missing/broken posters use a neutral fallback; place RelatedMovies after MoreWithPanel and before FileSection. Full rationale in CONCLUSIONS.md.


## Phase 4 Step 8 [assumed]
`MovieCarousel` renders its right-aligned `Previous`/`Next` button row inside the carousel region above the scroll viewport rather than inline in the caller's `<h2>` heading row, preserving the `{ label, children }` reusable component boundary for DEV-418. Full ruling in CONCLUSIONS.md.
