# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project overview

Blazor Web App for testing .NET regular expressions in the browser (hosted at https://dotnet-regex.com/). All regex work runs client-side in WebAssembly. Pages are static server-rendered components, and only the regex tester is interactive (`InteractiveWebAssembly`). Pages are rendered to HTML once at build time, so production is a static site: `deploy/cloudflare/export_static.py` turns the published + prerendered output into plain files, and `.github/workflows/deploy-cloudflare.yml` deploys them to Cloudflare Workers static assets (`dotnet-regex.com`, `www`). The Docker images are still built for self-hosting.

Target framework: **.NET 11** (`net11.0`) for all projects.

## Layout

- `BlazorWasmRegex/Client/` — interactive WebAssembly part. Main component is `Shared/RegexTester.razor` (+ `.razor.cs`): two-pane workbench with the pattern field, options, samples, replacement, test editor with gutter, live evaluation, status bar, result tabs, export menu and local-storage persistence. The result views are in `Shared/Results/`; small shared components (`Icon`, `CopyButton`, `EmptyState`, `HighlightedLine`, `MatchRuns`, `StatusBar`, `OptionTooltip`) are in `Shared/` and are also used by the server's static layout. DI registration in `Program.cs` (`ConfigureServices`, also called by the server for prerendering). `wwwroot/` holds `css/app.css`, `js/app.js`, icons, `robots.txt` and `sitemap.xml`.
- `BlazorWasmRegex/Shared/` — UI-independent logic:
  - `Services/RegexEvaluator.cs` (`IRegexEvaluator`) — runs a regex over each test line (matches with all groups, split, replace with inserted-text segments) and builds the highlight runs. Result models are in `Models/EvaluationResult.cs`.
  - `Services/PatternHighlighter.cs` — lexer that colors the pattern field (group colors match the results) and maps `RegexParseException` offsets to the character to underline.
  - `Services/RegexOptionsService.cs` (`IRegexOptionsService`) — options mapping, validation, `GetDisabledReason`, storage format. `Models/RegexOptionInfo.cs` is the option catalog (labels, flags, tooltips).
  - `Services/MatchExporter.cs` — JSON/CSV/text exports.
  - `Services/SampleRegexLibrary.cs` — sample patterns + sample test data.
- `BlazorWasmRegex/Server/` — Blazor Web App host; `Dockerfile` (x64) and `Dockerfile-Arm`. Containers expose ports 8080/8081.
  - `Components/` — `App.razor` (theme script before first paint, SVG icon sprite), `Routes.razor`, `Layout/` (MainLayout, AppHeader, AppFooter), `Pages/` (Home, About, NotFound), `PageMeta.razor` (title/description/canonical/OG tags), `RegexCheatSheet.razor` (click a token to insert it). All statically rendered.
  - `Program.cs` has three modes: **live** (`dotnet run`, renders per request), **prerender** (`--PrerenderTo <dir>`, `Prerenderer.cs` saves every page plus the generated `_framework/resource-collection*.js` files, then exits) and **static** (a `prerendered/` folder exists under the content root; `StaticPages.cs` serves it from memory and never renders components).
- `tests/RegexService.Tests/` — xUnit tests for the Shared services.
- `deploy/cloudflare/` — `export_static.py` (writes every static-asset URL from `BlazorWasmRegex.Server.staticwebassets.endpoints.json` as a real file, plus the prerendered pages and `_framework/resource-collection*.js`), `_headers`, and `wrangler.jsonc` (static-only Worker `dotnet-regex`, 404 page, custom domains). `site/` is the generated output (ignored).
- `.github/workflows/` — `deploy-cloudflare.yml` (tests, publish, prerender, export, `wrangler deploy` on pushes to `main`; PRs export only), `create-images.yml` (tests, then x64/ARM images to ghcr.io) and `cleanup-images.yml` (image retention). There is no build tool; CI calls `dotnet` and Docker directly.
- `docs/release-notes/` — per-upgrade/release notes (what changed, why, how it was verified, known issues).

## Common commands

```bash
dotnet build BlazorWasmRegex2.sln
dotnet test tests/RegexService.Tests
dotnet run --project BlazorWasmRegex/Server   # run app locally (live rendering)

# production mode locally: publish, prerender, then run from the publish folder
dotnet publish BlazorWasmRegex/Server -c Release -o out
(cd out && dotnet BlazorWasmRegex.Server.dll --PrerenderTo prerendered && dotnet BlazorWasmRegex.Server.dll)

# static site as deployed to Cloudflare (after the prerender step above)
python3 deploy/cloudflare/export_static.py out deploy/cloudflare/site
(cd deploy/cloudflare && npx wrangler@4.160.1 dev)   # serves it with the Workers runtime

# build an image locally (VERSION/SOURCE_REV default to 0.0.0.1/local)
docker build -f BlazorWasmRegex/Server/Dockerfile --build-arg VERSION=1.2.3.4 --build-arg SOURCE_REV=abc1234 -t regex-tester .
```

CI (`.github/workflows/create-images.yml`) pushes `ghcr.io/<owner>/regex-tester:x64` / `:arm`, with a `-<branch>` suffix off main, on pushes to `main` and `feature/**`. Pull requests build without pushing. Only the latest image per tag is kept; `cleanup-images.yml` removes old versions.

## Conventions and gotchas

- Put testable logic in `BlazorWasmRegex/Shared` services behind an interface, register it in `Client/Program.cs`, and cover it with tests in `tests/RegexService.Tests`. Keep `.razor` components thin.
- Regexes are built by `IRegexOptionsService.TryCreateRegex` (`Shared/Services/RegexOptionsService.cs`), which requires a match timeout. `RegexTester.razor.cs` passes its 2 s `RegexTimeout` to guard against catastrophic backtracking and catches `RegexMatchTimeoutException`. Keep the timeout required in `TryCreateRegex` (no overload without it), make every caller pass it, and keep all regex work inside `IRegexEvaluator.Evaluate` (it materializes matches, splits and replacements) so timeouts are thrown inside the `try` block, never during rendering.
- User state (pattern, tests, options) is persisted in browser `localStorage` through `ILocalStorageService` (`Shared/Services/LocalStorageService.cs`, with the `Client/Services/JsBrowserStorage.cs` JS interop wrapper). It replaced `Blazored.LocalStorage` and keeps its on-disk format: each value is stored as a JSON-encoded string, and regex options are a JSON object inside that string. Keys: `REGEX_SESSION_STORAGE_KEY`, `TESTS_SESSION_STORAGE_KEY`, `REGEX_OPTIONS`, `LIVE_MODE`; the theme is stored by `app.js` as plain `rx-theme`. Keep stored formats backward compatible or handle migration. Storage failures are swallowed, so the app works without persistence.
- The production site is static files on Cloudflare: anything the server does at request time is not available there. A new URL that the server answers dynamically (rather than from the static web assets manifest or a prerendered page) must also be handled in `export_static.py`. Check a change with `wrangler dev` on the export before relying on it.
- Pages are rendered once at build time, so nothing in them may depend on the request (no `NavigationManager.BaseUri`, query strings, cookies or per-user data); `Prerenderer` fails the build if a page contains its own address. Per-user state belongs in the interactive `RegexTester` (via `localStorage`).
- Adding a page: add the `.razor` page under `Server/Components/Pages` with a `<PageMeta>`, add its route to `StaticPages.Pages` and to `wwwroot/sitemap.xml`. Interactive components go in the Client project and are used with `@rendermode="InteractiveWebAssembly"`; they are prerendered on the server, so guard JS interop with `RendererInfo.IsInteractive`.
- UI: no CSS framework. `wwwroot/css/app.css` comes from the OpenDesign prototype: design tokens as CSS custom properties on `:root` with a `[data-theme="dark"]` override, and BEM-style component classes (`.pattern-field`, `.option-chip`, `.test-editor`, `.result-tab`, `.ref-card`, ...). Use the tokens instead of hard-coded colors and keep both themes and the 16px phone gutter working (no horizontal scroll at 360px). Icons come from the sprite in `App.razor` via `<Icon Name="..."/>`.
- `js/app.js` handles behavior that must also work on the static pages or without a round trip, using document-level delegation: theme toggle (`[data-theme-toggle]`), copy buttons (`[data-copy]`), hover linking of a match across views (`[data-m]`), cheat-sheet insertion (`[data-insert]`, dispatches `input` so `@bind` picks it up) and `regexTester.attach` (pattern/editor scroll sync, gutter row heights for wrapped lines).
- Versioning: `Directory.Build.props` (committed) holds the default `Version`. The Dockerfiles take `VERSION` and `SOURCE_REV` build args and pass them as `-p:Version` / `-p:SourceRevisionId`; after publishing they run the prerender step. Do not reintroduce build steps that rewrite tracked files.
- Dependabot opens NuGet update PRs; keep package versions consistent across projects.
- Update `README.md` when user-facing features change.
