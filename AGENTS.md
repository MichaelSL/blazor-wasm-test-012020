# AGENTS.md

Guidance for AI coding agents working in this repository.

## Project overview

Blazor WebAssembly app for testing .NET regular expressions in the browser (hosted at https://dotnet-regex.com/). All regex work runs client-side in WASM; the ASP.NET Core server only hosts the static client.

Target framework: **.NET 11** (`net11.0`) for all projects.

## Layout

- `BlazorWasmRegex/Client/` — Blazor WASM UI. Main component is `Shared/RegexTester.razor` (pattern input, options, samples, result tabs, replace, export, local-storage persistence). DI registration in `Program.cs` (`ConfigureServices`).
- `BlazorWasmRegex/Shared/` — UI-independent logic:
  - `Services/RegexService.cs` (`IRegexService`) — match/split/replace helpers.
  - `Services/HtmlHelperService.cs` (`IHtmlHelperService`) — match highlighting markup.
  - `Services/SampleRegexLibrary.cs` — sample patterns + sample test data.
- `BlazorWasmRegex/Server/` — ASP.NET Core host; `Dockerfile` (x64) and `Dockerfile-Arm`. Containers expose ports 8080/8081.
- `tests/RegexService.Tests/` — xUnit tests for the Shared services.
- `.github/workflows/` — `create-images.yml` (tests, then x64/ARM images to ghcr.io) and `cleanup-images.yml` (image retention). There is no build tool; CI calls `dotnet` and Docker directly.
- `docs/release-notes/` — per-upgrade/release notes (what changed, why, how it was verified, known issues).

## Common commands

```bash
dotnet build BlazorWasmRegex2.sln
dotnet test tests/RegexService.Tests
dotnet run --project BlazorWasmRegex/Server   # run app locally

# build an image locally (VERSION/SOURCE_REV default to 0.0.0.1/local)
docker build -f BlazorWasmRegex/Server/Dockerfile --build-arg VERSION=1.2.3.4 --build-arg SOURCE_REV=abc1234 -t regex-tester .
```

CI (`.github/workflows/create-images.yml`) pushes `ghcr.io/<owner>/regex-tester:x64` / `:arm`, with a `-<branch>` suffix off master, on pushes to `master` and `feature/**`. Pull requests build without pushing. Only the latest image per tag is kept; `cleanup-images.yml` removes old versions.

## Conventions and gotchas

- Put testable logic in `BlazorWasmRegex/Shared` services behind an interface, register it in `Client/Program.cs`, and cover it with tests in `tests/RegexService.Tests`. Keep `.razor` components thin.
- Regexes are built by `IRegexOptionsService.TryCreateRegex` (`Shared/Services/RegexOptionsService.cs`), which requires a match timeout. `RegexTester.razor` passes its 2 s `RegexTimeout` to guard against catastrophic backtracking and catches `RegexMatchTimeoutException`. Keep the timeout required in `TryCreateRegex` (no overload without it), make every caller pass it, and force any lazy regex work (`MatchCollection`, `Split` results) inside the `try` block so timeouts are not thrown during rendering.
- User state (pattern, tests, options) is persisted in browser `localStorage` through `ILocalStorageService` (`Shared/Services/LocalStorageService.cs`, with the `Client/Services/JsBrowserStorage.cs` JS interop wrapper). It replaced `Blazored.LocalStorage` and keeps its on-disk format: each value is stored as a JSON-encoded string, and regex options are a JSON object inside that string. Keep stored formats backward compatible or handle migration. Storage failures are swallowed, so the app works without persistence.
- UI uses BlazorStrap (Bootstrap 5).
- Versioning: `Directory.Build.props` (committed) holds the default `Version`. The Dockerfiles take `VERSION` and `SOURCE_REV` build args, pass them as `-p:Version` / `-p:SourceRevisionId`, and append `// build: $SOURCE_REV` to the container copy of `service-worker.published.js`. Do not reintroduce build steps that rewrite tracked files.
- Dependabot opens NuGet update PRs; keep package versions consistent across projects.
- Update `README.md` when user-facing features change.
