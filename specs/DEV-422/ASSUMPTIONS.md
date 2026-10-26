# DEV-422 — Assumptions

- `[assumed]` **Outer connection start / post-close retry schedule (Q3, Q6)**: Keep `@microsoft/signalr`'s default `withAutomaticReconnect()` delay schedule (`0ms, 2000ms, 10000ms, 30000ms`) for in-session reconnect attempts, and use a single cancellable outer retry schedule of `1000ms, 2000ms, 4000ms, 8000ms, 16000ms`, capped at `30000ms` thereafter until the hook is disposed, when initial `start()` fails or automatic reconnect is exhausted (`onclose`).
