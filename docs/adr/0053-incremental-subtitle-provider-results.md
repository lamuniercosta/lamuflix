---
status: Proposed
---

# Stream subtitle search results as providers settle

DEV-552 needs the picker to show results as each provider answers. The DEV-551 JSON search waits for every provider for the requested language. The existing search operation therefore offers an opt-in `text/event-stream` representation using ASP.NET Core 10 SSE. Both representations use one server fan-out per language, and existing JSON callers retain their response semantics.

The stream emits one settled outcome and its candidates per configured provider, then a terminal event with the settled count. The terminal event lets the picker distinguish an interrupted search from a complete one while retaining results already shown. Pre-stream validation and no-usable-provider errors use DEV-551's JSON behavior. Disconnect cancels outstanding provider calls. Named C# payloads flow through OpenAPI into generated TypeScript.

Each providerSettled event includes the required downloadAvailability field, reusing SubtitleDownloadAvailabilityDto. The server takes one snapshot when the stream opens and repeats it unchanged on every providerSettled event. The picker uses the snapshot as soon as candidates appear so read-only searches can show candidates and the reason while keeping download actions disabled. The same search supplies that information even if the stream later ends early.

The picker sends each checked candidate to the existing download route in visual order with one request active at a time. Each candidate keeps its provider binding and its own outcome, and the picker reloads the track list once after the batch settles.

Decision basis: DEV-552 Wanted 1, 4 and 8, DEV-551 ADR 0052, and DEV-552 conclusions Q1, Q4, Q5 and Q8.
