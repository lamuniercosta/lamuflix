# DEV-388 - Assumptions

- [assumed] Q2: The delete confirmation's wording and its placement on the movie details screen follow Keel's/Quill's recommendation at spec stage. After a delete, the SPA navigates back to the library view.
- [assumed] Q2 recommendation at spec stage: the details screen gains a control labeled "Remove from library", placed with the existing details actions and distinct from Play and watchlist. Confirmation asks "Remove this movie from the library?" with the body "The movie leaves the library. The file on disk stays." Confirm is "Remove" and cancel is "Cancel". Failure copy is "Could not remove this movie." and the retry label is "Try again".
