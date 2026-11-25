# Changelog

All notable changes to LamuFlix are documented in this file. This entry is written for the home-cluster owner.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [v0.1.0] - 2026-11-25

### Added

- **Library browse and filters.** Browse the movie library with text search, sorting, pagination, and filters for genre, cast, director, collection, year, runtime, rating, and enrichment status.
- **Movie details.** Open a movie for its metadata, cast, trailer, related titles, watchlist and review actions, plus manual IMDb matching when an imported title needs to be linked by hand.
- **Watchlist.** Add or remove movies from the watchlist from the library and details views; watchlist state follows the movie through browsing.
- **OMDb and TMDb enrichment.** Imported titles are enriched from OMDb and supplemented from TMDb with keywords, cast, collection, trailer, and recommendation signals. Enrichment runs through a durable queue with claim leases, retry, and dead-letter handling.
- **Recommendations.** `For You` and per-title recommendations are ranked from liked, watched, and watchlisted movies, excluding disliked and already-watched candidates.
- **In-browser playback.** Stream movies in the browser with play/pause, seeking, mute, speed, and fullscreen controls.
- **Subtitles.** Existing sidecar tracks are discovered, listed, and served as WebVTT, including an exact-baseline `.srt` sidecar beside the video. OpenSubtitles search and save into `Subs` is an explicit, per-host opt-in.
- **LocalPlay.** A host-only, fail-closed `Features:LocalPlay` opt-in can launch a configured external player for proven loopback requests. It stays off in Compose and rejects unproven or remote requests.
- **Home deployment.** The single-node kind cluster runs under Argo CD with externalized settings and secrets, digest-pinned image promotion, a `RELEASE.json` promotion record, and the app available at `http://127.0.0.1:3100`.

[Unreleased]: https://github.com/lamuniercosta/lamuflix/compare/v0.1.0...HEAD
[v0.1.0]: https://github.com/lamuniercosta/lamuflix/releases/tag/v0.1.0
