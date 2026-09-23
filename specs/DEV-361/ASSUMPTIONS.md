# DEV-361 assumptions

- [assumed] For a proven external MVC binder false positive in `FilmesFilterViewModel`, the local suppression may use a `// ReSharper disable <InspectionId>` / `// ReSharper restore <InspectionId>` bracket with a one-line reason such as "Written by the ASP.NET Core MVC model binder". The controller's `[FromQuery]` parameter is the expected writer (`FilmesController.cs:25`); no JSON deserializer is established for these three hits. The ticket fixes the required per-type/member scope and reason; this records only the exact wording/syntax choice (`task-DEV-361:16-18,28`; Patron Q4).
- [assumed] Replacement wording for the stale `.editorconfig:69,72,74` rationale comments (T013 Sentry F3): state that DEV-361 restored each key to `warning` and name the DEV-361 fix or member-scoped suppression, not a follow-up. Exact text is the implementer's.

No structural decision is assumed. The B1 reversal remains the owner checkbox in `brief.md` and `spec.md`.
