# DEV-538 assumptions

- [assumed] Patron ratified the ticket defaults of four seed rows and six cards per row as the presentation limits (`ticket-text.txt:52,80-81`).

- [assumed] Series ordering uses ascending known year, unknown years last, then API rank. Equally sized seed groups break ties by first API appearance. Overflow from themed-row or seed-row limits goes to More for you in API rank order. Patron ratified this policy in Q3.

- [assumed] Freeze the initial themed rows through More for you paging and Swap. Deduplicate all follow-up results against visible movie IDs and retain rendered rows when a follow-up is empty. Patron ratified this policy in Q4.

- [assumed] Render a labelled More for you section with Load more when it starts with zero cards and hasMore is true. Mount its carousel at one card and omit the section when it has neither cards nor available controls. Patron ratified this policy in Q5.

- [assumed] Use “No recommendations closely match the movies you have liked, watched, or added to your watchlist yet.” for rule 6b and “Browse the library” for both library links. Patron ratified the copy in Q6.

- [assumed] More for you keeps the landed DEV-517 control label (“Replace {title}” / “Replace”) for Swap, because DEV-517 owns that control; DEV-538 only scopes where it appears (More for you cards only) and moves Load more inside the More for you region.
