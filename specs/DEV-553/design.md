# DEV-553 interface design — grouped subtitle selector

Authority: `spec.md` US3/US4, FR-008, FR-012, FR-013; `contracts/subtitle-tracks.md`
Display boundary; `CONCLUSIONS.md` Q4/Q5; `ASSUMPTIONS.md` taste defaults.
Incumbent: `web/src/features/playback/SubtitleSelector.tsx`,
`useSubtitlePlayback.ts`, `SubtitlePanel.tsx`, `SubtitlesTab.tsx`.
Mode: Operate refinement — incumbent identity, behavior, and copy stay;
only the track list gains language groups and composed labels.

## Route map

No new route. No URL change. The selector lives where it does today:

- Player (`BrowserPlayer`) → subtitles panel (`SubtitlePanel`,
  `activeTab === 'subtitles'`) → subtitles tab (`SubtitlesTab`: search /
  download controls plus selector host) → `SubtitleSelector`.
- Data enters through `useSubtitlePlayback(movieId)`, which owns
  `GET /api/movies/{id}/subtitles` and `refreshAfterDownload`.
- Download flow stays in `SubtitlesTab`; on success it hands the returned
  track DTO to `refreshAfterDownload`, which reloads the list.

## Layout wireframe

Selector panel, ready state with two languages (English first-appearing):

```text
+-- Subtitles ----------------------------------+
|  ( ) Off                                      |
|                                               |
|  English                                      |
|  (•) Dune Part Two · OpenSubtitles            |
|  ( ) Dune Part Two 1080p BluRay · SubDL       |
|  ( ) Film-en                                  |
|                                               |
|  Portuguese                                   |
|  ( ) Dune Parte Dois · OpenSubtitles          |
+-----------------------------------------------+
```

`(•)` selected radio, `( )` unselected. `Off` sits above all groups,
outside any heading. `Film-en` is the hand-added fallback label —
wire `label` used verbatim because provenance is absent.

Loading, error, and empty states keep today's full-panel copy
(`SUBTITLES_LOADING_COPY`, `SUBTITLES_UNAVAILABLE_COPY` + retry,
`SUBTITLES_EMPTY_COPY`); groups render only in the ready state with
at least one track.

Narrow widths: groups stack vertically in first-appearance order;
no side-by-side columns at any breakpoint. Rows wrap to two lines
with the check mark pinned to the first line.

## Component tree

```text
BrowserPlayer
└── SubtitlePanel (tabs: subtitles | options; unchanged)
    └── SubtitlesTab (download controls + selector host; unchanged)
        └── SubtitleSelector (CHANGED: flat choices → grouped choices)
            ├── Off option (role="radio", first, ungrouped; unchanged)
            └── LanguageGroup[] (NEW presentational grouping)
                ├── group heading (language name, non-interactive)
                └── track radios (existing button[role="radio"] pattern)
useSubtitlePlayback (CHANGED: selection-by-id through reorder/relabel)
subtitleCopy (unchanged copy; no new strings required)
```

Grouping is a pure view derivation inside `SubtitleSelector`: partition
the incoming `tracks` array by the wire `track.language` in first-appearance
order, preserving each item's relative order. The wire `language` is
discovery-derived and authoritative for grouping; the persisted record's
stored candidate language is provenance only and never changes the group,
even when it differs from the wire value. No sorting in web —
backend order is authoritative (FR-010/FR-011). Display text is derived
per track by one function: return `` `${releaseName} · ${provider}` ``
only when both `provider` and `releaseName` are non-null/non-empty;
otherwise return the wire `label`. Partial provenance (one field known)
falls back to the wire `label` — never a half-composed label — while
the known field still rides the wire DTO. The middle dot is
ticket-mandated display copy (`release name · provider`), not decoration.

## Component states

| State | Render | Notes |
|---|---|---|
| Loading | `SUBTITLES_LOADING_COPY`, `aria-busy="true"` | Unchanged; no groups yet |
| List error | `SUBTITLES_UNAVAILABLE_COPY` + retry button | Unchanged; `onRetryList` |
| Empty | `SUBTITLES_EMPTY_COPY` (or unavailable when content errored) | Unchanged; Off not shown with zero tracks, as today |
| Ready, enriched | Heading per language; `release name · provider` rows | Canonical provider ID shown verbatim (`ASSUMPTIONS.md`) |
| Ready, fallback | Wire `label` rows: hand-added sidecars, null/null provenance, partial provenance, failed-metadata-write responses | Indistinguishable in shape from today's rows |
| Off | Single radio above all groups, `SUBTITLES_OFF_COPY` | Never inside a language group (`ASSUMPTIONS.md`) |
| Content error | `SUBTITLES_UNAVAILABLE_COPY` footnote under the list | Unchanged; list stays interactive |
| Selection refresh | Downloaded track stays checked across reload by server track id through reorder/relabel | `refreshAfterDownload` + id match; newer in-flight manual pick or Off wins via existing generation/attempt guards; absent track keeps existing handling (FR-013) |
| Post-delete reload | Row disappears; if it was selected, existing absent-track handling applies | Row excluded discovery-side; no client tombstone |

Grouping edge cases: a language with one track still gets its heading
(US3 acceptance: heading per language). The discovery-derived wire
`language` is the group key exactly as the wire sends it, even when the
stored candidate language in the record differs; no normalization or
alphabetical sort — first-appearance order governs, even when that order
looks unsorted. Stored language stays provenance only.

## Data needs (endpoint + fields)

Web consumes generated types only (`components['schemas']['SubtitleTrackDto']`
in `web/src/api/types.ts`); no hand-rolled duplicates.

| Need | Endpoint | Fields consumed |
|---|---|---|
| Track list | `GET /api/movies/{id}/subtitles` | `id`, `label`, `language`, `isDefault`, `provider` (nullable), `releaseName` (nullable) |
| Download result | existing download route (unchanged path/status) | same six fields; fallback responses carry null `provider`/`releaseName` with discovery-derived `label` |
| Subtitle bytes | `subtitleContentUrl(movieId, trackId)` | opaque content fetch; no DTO fields |
| Grouping key | derived client-side | discovery-derived wire `language` first-appearance order, even when stored candidate language differs (stored language is provenance only); order within group is wire order |

Ranking evidence (hash-match, match percent, download UTC, relative
path) never reaches the wire and is not a web data need.

## Accessibility

- One `radiogroup` labelled by the movie title (`subtitleSelectLabel`),
  preserving the incumbent roving-tabindex + arrow/Home/End keyboard map.
  Language headings are non-interactive headings associated with their
  group (`role="group"` + `aria-labelledby`); they are never focusable
  and never steal arrow-key navigation.
- `Off` remains the first radio in tab/arrow order so keyboard and
  screen-reader users meet the escape hatch before any group.
- Selection state rides `aria-checked` on each radio; the check glyph
  stays `aria-hidden`. After a download reload, focus stays where it
  was — selection moves programmatically to the downloaded track, but
  focus is not forced there; screen-reader users discover the change
  through the existing `role="status"` region.
- Loading announces through `role="status"` + `aria-busy`; list error
  pairs the status text with a labelled retry button.
- Composed labels expose the full `release name · provider` string as
  the radio's accessible name; no truncated `aria-label` variants.
- Contrast: enriched and fallback rows share one text color meeting
  4.5:1 against the panel surface; provenance is never signaled by
  color alone (the composed string carries it in words).
- Reduced motion: the selector adds no animation; any check/appear
  transition elsewhere must honor `prefers-reduced-motion`.
- Touch/pointer: each radio row keeps a minimum 44px target height;
  wrapped two-line labels keep the full row clickable.
- Responsive: single-column group stack at all widths; headings never
  detach from their group (no orphaned heading at a column break).
