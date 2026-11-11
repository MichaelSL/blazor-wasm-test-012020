# Release notes: UI/UX redesign

**Date:** 2026-11-11
**Change:** New look and workflow for the tester, designed as a prototype in OpenDesign and ported to the Blazor app. Bootstrap 4, open-iconic and BlazorStrap are removed.

## Why

The UI was the default Blazor template: a purple sidebar, stacked Bootstrap 4 form fields, five same-sized buttons and results hidden in tabs below the fold. It looked dated and needed a click per test.

## What changed

- **Layout:** a compact top bar (brand, nav, GitHub link with the version, theme toggle) replaces the sidebar. The page has a footer with a privacy note. The tester is a two-pane workbench, with inputs on the left and results on the right. The results pane is sticky on desktop and the panes stack below 1024px. Header, content and footer share one 16px gutter on phones, with no horizontal scrolling at 360px.
- **Live evaluation:** results update about 200 ms after typing stops. The "Live" switch (stored as `LIVE_MODE`) turns this off; Test and Ctrl+Enter still run on demand.
- **Pattern field:**
  - The pattern is shown as `/pattern/flags` and syntax-colored by `PatternHighlighter`.
  - Capture-group parentheses use the same six colors as the highlighted captures and the Groups view.
  - Parse errors underline the failing character (or mark the end for missing `)`/`]`) and show the .NET `RegexParseError` with the offset.
  - Timeouts have their own amber state.
- **Options:**
  - Ignore case, Multiline and Singleline are chips. The other seven options are under "Advanced", which has a count badge.
  - Options that can't be combined with the current selection are disabled, with the reason (`IRegexOptionsService.GetDisabledReason`, derived from `Validate`).
  - Every option has a tooltip.
- **Test editor:** matches are highlighted inside the textarea through a mirrored backdrop, and a gutter shows the match count per line. Long lines soft-wrap in both layers, and gutter rows follow the wrapped height.
- **Per-line evaluation (behavior change):** each test line is matched independently and keeps its line number. Lines are no longer trimmed or de-duplicated; empty lines are kept but not evaluated. `RegexEvaluator` replaces `RegexService` and `HtmlHelperService`, and their tests were replaced with tests for the new services.
- **Results:**
  - The Matches, Split, Table, Groups and Replace tabs show counts.
  - Hovering a match highlights it in every view.
  - Values have copy buttons.
  - Replace shows a before/after diff and updates live.
  - Every view has a designed empty state. The prerendered page shows a loading state with a skeleton.
- **Export:** one menu instead of three buttons. The JSON, CSV and text formats are unchanged (`MatchExporter`). In JSON, a repeated test line is exported once, as before.
- **Samples:** the Email, Phone and URL samples now use named groups, to show the group colors. A first visit with no saved state loads the Email sample.
- **Quick reference:** the cheat sheet is a card grid. Clicking a token inserts it at the caret, group tokens wrap the selection, and substitutions go into the replacement field (`app.js`, `[data-insert]`).
- **Theme:** light and dark tokens. The initial theme follows the system setting, and the toggle is saved as `rx-theme`. An inline script in `App.razor` applies it before first paint.
- **Assets:** removed `css/bootstrap` (772 KB) and `css/open-iconic` (192 KB). Icons are an inline SVG sprite.

## How it was verified

- `dotnet build BlazorWasmRegex2.sln` succeeds with no warnings. `dotnet test tests/RegexService.Tests` passes (137 tests), including new tests for `PatternHighlighter`, `RegexEvaluator`, `MatchExporter` and `GetDisabledReason`.
- Checked in Chrome with `dotnet run`:
  - the Email sample (3 matches, Split 13, Groups 6)
  - a parse error (underline and offset), a timeout (`^(a+)+$` on 32 `a`s and `!`)
  - option locking (NonBacktracking disables Any NewLine, Right to left and ECMAScript)
  - Replace, every tab, hover linking between the editor and the results, cheat-sheet insertion into both fields, the Live toggle, the dark theme and the About page
- At 360px the page doesn't scroll sideways, and every block sits on the 16px gutter. Gutter rows match wrapped line heights (24/72/24/24 px), and highlights stay inside the editor.
- Published to a folder, ran `--PrerenderTo prerendered`, then served it statically: `/` and `/about` return 200, an unknown path returns 404, the prerendered tester shows the loading state with disabled controls, and the page becomes interactive with no console errors.

## Known issues / follow-ups

- Selecting a sample keeps your own test lines, as before. It only replaces them when they are empty or another sample's data.
- Live evaluation runs on the UI thread. A pathological pattern can still block typing for up to the 2 s timeout after each pause. Turn Live off for such patterns.
- Docker images (x64/ARM) were not rebuilt locally for this change. CI builds them.
