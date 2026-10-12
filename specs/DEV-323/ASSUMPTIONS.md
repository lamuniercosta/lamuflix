# DEV-323 - Taste assumptions (Patron)

- [assumed] Rating badges are labelled `IMDb`, `Rotten Tomatoes` and `Metacritic`. Each shows the stored value as-is (IMDb `x.x/10`, RT stored string, Metacritic `nn`), and a badge is hidden when its value is null.
- [assumed] Runtime displays as `2h 16m` (minutes only, `45m`, when under one hour). The year displays as plain `YYYY`.
- [assumed] Directors and cast are comma-separated lists labelled `Director`/`Directors` and `Cast`. All stored actors are shown.
- [assumed] The poster `alt` is `<title> poster`. A missing poster renders a neutral placeholder block with no text.
- [assumed] The Play button label is `Play`, and it reads `Starting…` while in flight. On 204 the status reads `Playback started.`
- [assumed] The 403 modal title is `Playback unavailable`, its body is `Local playback is disabled on this server.`, and its button is `Close`.
- [assumed] Load states read `Loading movie…`, `Movie not found.` and `Couldn't load this movie. Try again later.` The no-metadata notice reads `No metadata available yet.`
- [assumed] The Play 404 message is `This movie is no longer in the library.` The generic Play error reads `Couldn't start playback. Try again later.`
