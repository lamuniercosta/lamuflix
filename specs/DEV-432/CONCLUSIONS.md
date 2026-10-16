# DEV-432 alignment conclusions

## Decision 1: Responsive filter placement

### Full decision exchange

Assistant: DEV-432 is started in its isolated worktree. DEV-433's prerequisite is already present; tools restored successfully.

Assistant question: "First design decision: use a collapsible filter sidebar at widths ≥768px, and a modal drawer below 768px?"

Assistant recommendation: "I recommend this because filters remain prominent on desktop while preserving poster space on phones. The drawer adds focus trapping and restoration work."

Assistant process explanation: "The mandatory alignment skill requires asking decisions 'one at a time, waiting for feedback' before proceeding."

User: "Yes"

### Confirmed decision

Use a collapsible filter sidebar at viewport widths of 768px and above, and a modal filter drawer below 768px.

### Rationale

Keep filters prominent on desktop and preserve poster space on phones.

### Implications

- At the acceptance widths, 375px uses the drawer; 768px and 1440px use the sidebar.
- The modal drawer must trap focus while open and return focus to its trigger when closed.
- The sidebar requires a collapse control.
- Default sidebar state and any persistence policy remain undecided.
- Further alignment decisions remain open; this decision does not conclude the grill or authorize implementation.

---

## Decision 2: Initial filter visibility and persistence

### Full decision exchange

Assistant: "Recorded in CONCLUSIONS.md."

Assistant question: "Should filters start expanded on desktop and closed on mobile, without remembering collapse state across reloads?"

Assistant recommendation: "I recommend those defaults: desktop filters stay discoverable, phones retain browsing space, and persistence adds little value initially."

User: "Yes"

### Confirmed decision

The desktop sidebar starts expanded. The mobile drawer starts closed. Do not persist filter-panel visibility across reloads.

### Rationale

Keep desktop filters discoverable and preserve browsing space on phones without adding persistence for panel visibility.

### Implications

- A fresh page load at 768px or wider shows the expanded sidebar.
- A fresh page load below 768px shows the library with the filter drawer closed.
- Sidebar collapse and drawer visibility are transient UI state.
- This decision concerns panel visibility only; existing filter values and URL behavior remain subject to the task's behavior-preservation requirement.
- The initial filter visibility question left open in Decision 1 is resolved by this decision.
- Further alignment decisions remain open; the grill has not concluded.

---

## Decision 3: Visual direction

### Full decision exchange

Assistant: "Recorded."

Assistant question: "Keep the earlier browse design's visual direction: dark slate surfaces, warm white text, amber accents, and a serif LamuFlix wordmark?"

Assistant recommendation: "I recommend it, with system sans-serif for controls and metadata. It lets posters carry the color and requires no downloaded fonts. The tradeoff is a quieter catalogue feel."

User: "Yes"

### Confirmed decision

Use dark slate surfaces, warm white text, amber accents, a serif LamuFlix wordmark, and system sans-serif for controls and metadata. Use system font stacks without downloaded fonts.

### Rationale

Keep the surrounding interface quiet so movie posters carry the color, building on the earlier browse design's visual direction without an external font dependency.

### Implications

- Use specs/DEV-321/design.md section 1 as the visual reference for palette and system font stacks.
- Express the shared design through Tailwind theme tokens, as required by DEV-432.
- Validate contrast against DEV-432's WCAG AA requirement; an earlier proposed color is not proof of compliant contrast in every pairing.
- This adopts the referenced visual direction, not the earlier document's filter-band layout or historical scope restrictions. Decisions 1 and 2 govern filter placement and initial visibility.
- Further alignment decisions remain open; the grill has not concluded.

---

## Decision 4: Mobile drawer filter updates and closing

### Full decision exchange

Assistant: "Recorded."

Assistant question: "Should the mobile drawer keep live filter updates, with a 'Done' button that simply closes it?"

Assistant recommendation: "I recommend this to preserve current behavior: selections update results immediately, search stays debounced, and closing the drawer keeps those changes. The tradeoff is that users cannot cancel a batch of edits."

User: "yes"

### Confirmed decision

Keep live filter updates in the mobile drawer. Its Done button closes the drawer without applying, reverting, or otherwise changing the selected filters. Search remains debounced.

### Rationale

Preserve existing filter behavior while adding responsive placement. A staged Apply/Cancel workflow would change that behavior.

### Implications

- Filter selections update results while the drawer remains open.
- Done closes the drawer and retains the current filters.
- Closing the drawer is not a batch-edit cancellation action.
- Keep the focus trap and focus restoration established in Decision 1.
- Further alignment decisions remain open; the grill has not concluded.

---

## Decision 5: Top-bar search from movie details

### Full decision exchange

Assistant: "Recorded."

Assistant question: "On a movie's details page, should submitting the top-bar search return to the library, keeping the previous filters and replacing the search text?"

Assistant recommendation: "I recommend requiring Enter there, so typing doesn't unexpectedly leave the movie. Search within the library would remain debounced as it is today."

User: "Go with your recomendation"

### Confirmed decision

On a movie details page, typing into the shared top-bar search does not navigate away. Submitting the search with Enter returns to the library, preserves the previous browse filters, and replaces the search text. Search within the library remains debounced.

### Rationale

Avoid unexpectedly leaving a movie while the user is typing. Preserve the established live-search behavior when browsing the library.

### Implications

- Keep details-page search input edits local until submission.
- Carry the previous browse filters into the library search destination, replacing its text filter with the submitted search text.
- Client-side navigation and the details back link must retain the filtered library URL required by DEV-432.
- This decision defines details-page search behavior; it does not change the mobile drawer's live filter updates agreed in Decision 4.
- Further alignment decisions remain open; the grill has not concluded.

---

## Decision 6: Frozen scope and planning delegation

### Full decision exchange

Assistant: "Recorded."

Assistant question: "Freeze DEV-432 to these six areas?"

Assistant proposed scope:

1. Shared dark-theme tokens and typography.
2. Shell, search, and client-side navigation.
3. Responsive filters, removable chips, result count, and sorting.
4. Poster cards and loading, empty, and error states.
5. Movie details, actions, and return to the filtered library.
6. Responsive and keyboard accessibility, plus the README screenshot.

Assistant recommendation: "I recommend preserving existing APIs and behavior except for the agreed interactions. Recommendations, additional thumbnail work, and i18n stay excluded; anything else is a follow-up issue, not a finding in this round."

User: "Yes. Use a haiku subagent for the next steps; Let me know when you reach gate 1"

### Confirmed decision

Freeze the task to the six areas above. Preserve existing APIs and behavior except for the agreed interactions. Recommendations, additional thumbnail work beyond DEV-429, and i18n are excluded; anything else is a follow-up issue, not a finding in this round.

Use a Haiku subagent for the next planning steps. Prepare the specification, plan, tasks, and analysis, then notify the user at Human Gate 1. Implementation is not authorized by this instruction.

### Rationale and implications

- The agreed scope is countable and bounds the future review loop.
- Parent review must independently verify delegated artifacts.
- Stop at Human Gate 1 for the user's review of spec, plan, and tasks.
- The review closing bar and round cap still require the pending user decision before the grill can conclude.

---

## Decision 7: Review closing bar, round cap, and alignment close

### Full decision exchange

Assistant: "I'll record the frozen scope, use Haiku for the planning work, and stop at Gate 1 for your review. One review-policy decision still needs to be settled before the grill can close."

Assistant question: "For DEV-432's review policy, I recommend blocking Critical/High findings with a concrete failure scenario, with at most two review/fix rounds. At the cap, unresolved blockers remain reported and prevent clearance. Use this policy?"

User: "Use the recommended policy"

### Confirmed decision

Critical and High review findings with a concrete failure scenario block clearance. Allow at most two review/fix rounds. At the cap, report unresolved blockers and do not claim clearance. Keep the six-area scope approved in Decision 6; anything else is a follow-up issue, not a finding in this round.

### Rationale and implications

- The explicit closing bar, scope, and cap bound future code-review and ship-review loops.
- A round cap never converts an unresolved blocker into clearance.
- The user's direction to prepare the next steps through Gate 1, together with the settled decisions above, concludes alignment. Prepare spec, plan, tasks, and analysis; stop before implementation.
- No new domain terminology was introduced; the existing CONTEXT.md remains canonical. No hard-to-reverse architectural tradeoff requiring an ADR was selected during this alignment.
- The confirmed exchanges are now persisted in this append-only file and may be summarized without losing the decision record.

---

## Decision 8: Delegate model correction

### Full decision exchange

User: "I meant luna, not haiku"

### Confirmed correction

Use a Luna subagent for the next steps through Human Gate 1, replacing the Haiku delegation instruction in Decision 6. All settled scope, interaction, and review-policy decisions remain in effect.

### Implications

- The parent stopped the active Haiku CLI invocation before handing writing ownership to Luna.
- Haiku completed only its one-file write probe; no specification or checklist draft exists at this handoff.
- Continue with Luna and independently review its planning artifacts. Stop at Human Gate 1 before implementation.

---

## Decision 9: Existing metadata and playback contract boundary

### Full decision exchange

Assistant question: "The current details API exposes neither per-movie genres nor a playback-capability flag. I recommend keeping DEV-432 frontend-only: show the metadata available today and preserve Play's existing server-enforced 403 dialog, with genre data and proactive Play hiding deferred to backend follow-up work. Accept this boundary?"

Assistant options: "Keep the frontend-only boundary" or "Include backend contract changes".

User: "Keep the frontend-only boundary"

### Confirmed decision

Keep DEV-432 frontend-only. Present metadata provided by the current API. Defer per-movie genre data and proactive Play hiding to backend follow-up work. Preserve the current Play endpoint and its accessible 403 Playback unavailable dialog.

### Evidence and implications

- web/src/api/types.ts MovieMetadataResponse and src/LamuFlix.Api/Endpoints/LibraryResponses.cs expose no per-movie genres or playback-capability field.
- Genre facet data is not evidence of a genre association for a particular movie.
- The layout cannot invent missing genre or feature-capability data, and no API/generated type changes are authorized.
- This explicit accepted limitation narrows the original details metadata promise where the current contract does not supply it. Other available metadata remains required.
- The follow-up boundary is recorded here; no tracker issue was created by this decision.
