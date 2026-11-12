# Blazor WASM Regex Tester - AI Agent Instructions

The full, maintained guidance for AI agents is in [AGENTS.md](../AGENTS.md) at the repository root. Read it before changing code; it covers the layout, commands, CI and conventions.

Short version:

- Blazor Web App on **.NET 11**. All regex work runs client-side in WebAssembly; pages are prerendered to static HTML at build time and only `RegexTester` (Client project) is interactive.
- Testable logic lives in `BlazorWasmRegex/Shared` services behind interfaces (`IRegexEvaluator`, `IRegexOptionsService`, `ILocalStorageService`, plus `PatternHighlighter` and `MatchExporter`), registered in `Client/Program.cs` and covered by `tests/RegexService.Tests` (`dotnet test tests/RegexService.Tests`).
- Every regex is built through `IRegexOptionsService.TryCreateRegex` with the 2 s match timeout; all regex work happens inside `IRegexEvaluator.Evaluate`.
- No CSS framework: `Client/wwwroot/css/app.css` uses design tokens (light/dark) and BEM-style classes; icons come from the SVG sprite in `App.razor` via `<Icon Name="..."/>`. `js/app.js` handles the theme toggle, copy buttons, hover linking, cheat-sheet insertion and editor sync.
- CI: `.github/workflows/create-images.yml` tests, then builds x64/ARM images to `ghcr.io/<owner>/regex-tester:x64` / `:arm` (with a `-<branch>` suffix off main). Pull requests build but don't push.
- Update `README.md` when user-facing features change, and add release notes under `docs/release-notes/` for notable changes.
