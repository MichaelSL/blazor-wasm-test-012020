# Release notes: upgrade to .NET 11

**Date:** 2026-11-10
**Change:** .NET 10 (LTS) → .NET 11 (STS) for all projects, the build, CI and container images.

## What the upgrade gives us

### For users of the regex tester

The app runs the real .NET regex engine in the browser, so the runtime version *is* the product: patterns are now evaluated with .NET 11 semantics.

- **Regex correctness fixes in .NET 11:**
  - The `NonBacktracking` engine no longer takes super-linear time on certain nested-loop patterns, and returns correct results in cases that previously diverged from the backtracking engine.
  - The regex compiler and source generator handle `resumeAt` correctly when a conditional sits inside a loop body.
- **New `RegexOptions.AnyNewLine`:** `^`, `$` and `.` treat all Unicode newlines (`\r\n`, `\n`, `\u0085`, `\u2028`, `\u2029`) as line terminators. It is exposed in the UI as the "Any NewLine" option (see Step 4).

### For hosting and operations

- **Support window unchanged:** .NET STS releases are supported for 24 months, so .NET 11 is supported until **November 2028**, the same end date as .NET 10 LTS. Upgrading costs us no support time.
- **New container base:** .NET 11 images are published for **Ubuntu 26.04 "resolute"** only; there are no `noble` tags. Both Dockerfiles now use `aspnet:11.0-resolute-chiseled-extra` (x64) and `aspnet:11.0-resolute-chiseled-extra-arm32v7` (ARM). The arm32v7 chiseled image still exists, so the ARM self-hosted deployment keeps working.
- **Runtime/JIT improvements** (bounds-check and redundant-branch elimination, devirtualization, Runtime Async) apply to the ASP.NET Core server. It only serves static files, so expect little visible change.

### For development

- **Debugging no longer needs the WASM debug proxy middleware.** `UseWebAssemblyDebugging()` is obsolete in .NET 11 (`ASPDEPR011`); Visual Studio and VS Code launch WASM debugging directly. Removed the call and the `inspectUri` entries from both `launchSettings.json` files.
- **NUKE build moved to Nuke.Common 10.1.0** (from 8.3.0).

### Bundle size (measured)

Release publish of `BlazorWasmRegex.Server`, `wwwroot/_framework`:

| | .NET 10 | .NET 11 |
|---|---|---|
| Files (uncompressed) | 61 | 66 |
| Raw size | 11.43 MB | 12.25 MB |
| Brotli total (what browsers download) | 3.47 MB | 3.66 MB |
| Brotli `.wasm` only | 2.78 MB | 2.93 MB |

.NET 11 adds trimming for Blazor metrics/tracing and Hot Reload code, but for this app the net result is about **5% larger** (+0.19 MB brotli). It's not a reason to upgrade; worth re-checking on later 11.0.x servicing releases.

## Changes made

- `net10.0` → `net11.0` in all `.csproj` files (Client, Server, Shared, tests, `build/_build.csproj`).
- `Microsoft.AspNetCore.Components.WebAssembly*` packages 10.0.0 → 11.0.0.
- Dockerfiles: base images moved to `11.0-resolute-chiseled-extra[-arm32v7]`, SDK image to `sdk:11.0`.
- CI (`.github/workflows/create-images.yml`): `dotnet-version: 11.0.x`.
- `Server/Program.cs`: removed obsolete `app.UseWebAssemblyDebugging()`; removed `inspectUri` from launch profiles.
- NUKE build:
  - `Nuke.Common` 8.3.0 → 10.1.0 with API updates (`FileSystemTasks` static helpers replaced by `AbsolutePath` extension methods; `[Solution]` no longer takes `SuppressBuildProjectCheck`).
  - `<UseAppHost>false</UseAppHost>` in `build/_build.csproj`: under .NET 11, starting the build through the native apphost (which `dotnet run` does) fails Nuke's entry-assembly location check; starting it as `dotnet _build.dll` works.
  - `Test` target no longer passes `--no-build`. `Compile` builds only the server project, so the test assembly was never built and `./build.sh -Target Test` failed. This bug predates the upgrade.
  - `.nuke/build.schema.json` regenerated (it still listed the old `DockerLogin`/`DockerPassword` parameters).

## Verification

- `dotnet build BlazorWasmRegex2.sln -c Release`: succeeds, no new warnings.
- `dotnet test`: 5/5 pass on `net11.0`.
- `./build.sh -Target Test`: Restore, Compile and Test all succeed.
- `./build.sh -Target BuildDockerContainer`: x64 image builds; the container serves `/` and `_framework/blazor.webassembly.js` with HTTP 200.
- Browser test (Chrome) against the dev server and the Release Docker container. All of these work on .NET 11:
  - samples
  - match highlighting
  - Split list, Table and Capture Groups tabs, including named groups
  - Replace with `$1` and `${name}`
  - JSON/CSV/Text export
  - Ignore Case
  - invalid-pattern error message
  - 2 s catastrophic-backtracking timeout (fired at ~2.0 s)
  - local-storage restore after reload
  - About page
- The Release container shows no console errors. The `hot-reload-settings.json` 404 seen under `dotnet run` is a dev-only Hot Reload probe.
- Not verified locally: the ARM image build and a run on the ARM device.

## Fixed: result tabs

The result tabs (Matches / Split list / Table / Capture Groups / Replace) never worked as tabs. `RegexTester.razor` used `BSTabGroup`, `BSTabList`, `BSTab`, `BSTabLabel`, `BSTabContent` and `BSTabSelectedContent`, which don't exist in BlazorStrap 5.2. Razor emitted them as unknown HTML elements (36 `RZ10012` warnings), so every panel rendered stacked with no tab strip. This predates .NET 11, and the browser test made it visible. The markup now uses the real BlazorStrap 5 API: `BSTabWrapper` > `BSNav IsTabs="true"` > `BSNavItem` with `TabLabel`/`TabContent`, plus `BSTabRender`, with Matches active by default. The build now has zero Razor warnings. (This was Task 1 in the removed `docs/regex-tester-improvements.md`.)

## Follow-up modernisation

### Step 1: low-risk cleanup

- **Tests on xUnit v3:** `xunit` 2.6.2 (deprecated) → `xunit.v3` 4.0.3, `xunit.runner.visualstudio` 2.5.7 → 4.0.1, `Microsoft.NET.Test.Sdk` 17.11.1 → 18.11.1. No test API changes were needed. xunit.v3 4.x turns on Microsoft.Testing.Platform v2 by default, and the VSTest-based `dotnet test` (still the SDK default) refuses to run MTP v2 projects ("Testing with VSTest target is no longer supported…"). The test project therefore sets `<IsTestingPlatformApplication>false</IsTestingPlatformApplication>` and stays on VSTest through `xunit.runner.visualstudio`. Moving the repo to MTP mode (a `global.json` with `"test": { "runner": "Microsoft.Testing.Platform" }`) is a possible later step.
- **DevServer removed:** the Client no longer references `Microsoft.AspNetCore.Components.WebAssembly.DevServer`, and `Client/Properties/launchSettings.json` is gone. Its profiles started the client on its own, which needed DevServer. The Server project hosts the client, so run the app with `dotnet run --project BlazorWasmRegex/Server`.
- **Match timing fix:** `RegexService.GetMatches` timed `Regex.Matches()`, but `MatchCollection` is lazy. The reported time was about 0 ms, and the real matching (and any `RegexMatchTimeoutException`) happened later during rendering. It now reads `Count` on each collection inside the timed block, using `Stopwatch.GetTimestamp`/`GetElapsedTime`. A new test checks that a catastrophic-backtracking pattern with a 1 ms timeout throws from `GetMatches` itself. The test fails against the old code.
- **`RegexOptions.Compiled` removed** from `RegexTester.razor`. It has no effect in WASM, which only has the regex interpreter. The 2 s `RegexTimeout` is unchanged.
- **Server:** removed the unused `AddControllersWithViews()`/`MapControllers()`, since the project has no controllers. Razor Pages (the Error page) and static file serving are unchanged.
- **Verified:**
  - `dotnet build BlazorWasmRegex2.sln -c Release` gives 0 warnings and 0 errors.
  - `dotnet test tests/RegexService.Tests` passes 7/7.
  - `./build.sh -Target Test` succeeds and runs the 7 tests.
  - `dotnet run --project BlazorWasmRegex/Server` (Development profile) serves `/` and `/_framework/blazor.webassembly.js` with HTTP 200.

### Step 2: replace Blazored.LocalStorage

- **Why:** every Blazored.LocalStorage version has been unlisted from NuGet, and 4.3.0 is marked deprecated ("Legacy"). The app only needs to get and set three strings, so the package is replaced by a small in-repo implementation.
- **What changed:**
  - Removed the `Blazored.LocalStorage` 4.5.0 package, the `@using Blazored.LocalStorage` line and `AddBlazoredLocalStorage()`.
  - Added `Shared/Interfaces/IBrowserStorage.cs` (raw `localStorage` get/set) and `Shared/Interfaces/ILocalStorageService.cs`.
  - Added `Shared/Services/LocalStorageService.cs`, which owns the format and error handling.
  - Added `Client/Services/JsBrowserStorage.cs`, a thin wrapper over `localStorage.getItem`/`setItem` through `IJSRuntime`, so no new JS file is needed.
  - Both services are registered (scoped, like Blazored) in `Program.ConfigureServices`. `RegexTester.razor` now calls `GetStringAsync`/`SetStringAsync`.
- **Backward compatibility:** the format matches Blazored.LocalStorage 4.5.0, confirmed by decompiling the package with ilspycmd.
  - Blazored wrote `JsonSerializer.Serialize(value, new JsonSerializerOptions())`, with default options plus a TimeSpan converter that doesn't affect strings. A string is therefore stored as a quoted JSON string with the default encoder's escapes (`\n`, `\u00E9`, `\u003C`, `\u0022`, …).
  - On read, Blazored returned `null` for a missing, empty or whitespace-only value. Otherwise it deserialized the value and fell back to the raw text when it wasn't a JSON string.
  - The new service does the same, so existing `REGEX_SESSION_STORAGE_KEY`, `TESTS_SESSION_STORAGE_KEY` and `REGEX_OPTIONS` values load unchanged, and rolling back to a Blazored build can read what this version writes.
  - `REGEX_OPTIONS` stays a JSON object stored inside a JSON string, as before.
  - One deliberate difference: a `JsonException` at a path other than `$` falls back to the raw value, where Blazored rethrew it. No input reaches this path when the value is a string.
- **Storage unavailable:** if a read fails (for example a `JSException` when the browser blocks `localStorage`), the service returns `null`. A failed write is skipped. Only the first failure is logged to the console. The app keeps working, just without persistence. The wrapper suppresses the new .NET 11 analyzer warning BL0016 (unguarded JS interop call), with a comment, because the service handles these exceptions.
- **Verified:**
  - `dotnet build BlazorWasmRegex2.sln -c Release` gives 0 warnings and 0 errors.
  - `dotnet test tests/RegexService.Tests` passes 40/40. The 33 new tests cover:
    - round trips
    - exact Blazored-format output
    - reading Blazored-format values with escapes, unicode, CRLF/LF newlines and the double-encoded options JSON
    - raw non-JSON fallback, including the legacy pipe-delimited options format
    - missing, empty and `null` values returning `null`
    - storage exceptions
  - A scratch console app ran the real Blazored.LocalStorage 4.5.0 (with a fake `IJSRuntime`) side by side with the new service. Writes were byte-identical for 10 values, and reads were identical for 19 raw inputs, including malformed JSON, numbers, objects, arrays and comments.
  - In Chrome against `dotnet run --project BlazorWasmRegex/Server`:
    - Blazored-format values seeded into `localStorage` loaded correctly: pattern, unicode/newline test text and checkbox options.
    - New writes used the same escaped format.
    - With `window.localStorage` made to throw a `SecurityError`, the app loaded, matched normally and logged one "continuing without persistence" line.

### Step 3: MapStaticAssets and client-side fingerprinting

- **Server (`Server/Program.cs`):** `UseBlazorFrameworkFiles()` + `UseStaticFiles()` are replaced by `MapStaticAssets()`, and the `AddResponseCompression`/`UseResponseCompression` setup is removed. MapStaticAssets serves the build-time br/gzip files with ETags, so runtime compression isn't needed. Microsoft's docs say MapStaticAssets also replaces `UseBlazorFrameworkFiles`. The `.wasm`/`.dat`/`.js` content types come from the static web assets endpoint manifest.
  - The only `UseBlazorFrameworkFiles` feature that is gone is the `Blazor-Environment` header. Since .NET 10 the WASM runtime ignores it and takes its environment from the build (`WasmApplicationEnvironmentName`: Development for build, Production for publish). The client never reads its environment.
  - The Error page, HSTS and HTTPS redirection are unchanged.
  - `MapFallbackToFile("index.html")` still serves `/` and deep links. It now sends `Cache-Control: no-cache`, because index.html refers to fingerprinted files and a heuristically cached copy could point at files that no longer exist after a deploy.
- **Client fingerprinting:**
  - The Client csproj sets `OverrideHtmlAssetPlaceholders=true`.
  - `index.html` has `<link rel="preload" id="webassembly" />` and `<script type="importmap"></script>` in `<head>`, and loads `_framework/blazor.webassembly#[.{fingerprint}].js`.
  - At build/publish the SDK fills in the import map (with SRI hashes) and a preload link for `dotnet.<hash>.js`.
- **SDK workaround for hosted publish:** in a hosted publish (Server → Client), the .NET 11.0.100 SDK left the published `index.html` with its placeholders unreplaced: an empty importmap, a literal `#[.{fingerprint}]` in the script URL, and no `index.html` endpoint or .br/.gz files.
  - Cause: the SDK collects HTML files (`_HtmlStaticWebAssets`) only in the build-time target `ResolveHtmlAssetPlaceholdersBuildConfiguration`. The Server runs the Client's publish targets in a separate project instance where that target never ran.
  - A standalone `dotnet publish` of the Client works.
  - Fix: a small `_CollectHtmlAssetsForHostedPublish` target in the Client csproj points the publish step back at `wwwroot/index.html`. It does nothing when the SDK has already collected the files.
  - Remove the target once the SDK handles this case.
- **Not fingerprinted after publish:** `blazor.webassembly.js` keeps its plain name in the published index.html. The SDK's publish step only rewrites files fingerprinted by file name, and in .NET 11 `blazor.webassembly.js` ships from the package with an endpoint-only fingerprint. The same happens in a standalone Client publish. It is served with `no-cache` + ETag (304 on revalidation). Everything it loads (`dotnet.*.js`, all `.wasm`, ICU `.dat`) is fingerprinted and immutable. In a Development build the script tag is fingerprinted (`blazor.webassembly.ldl9n57ejg.js`).
- **NUKE targets:**
  - `SetVersion` (Directory.Build.props + service-worker.published.js stamp) and `CleanUpSwVersion` work unchanged; the source files were restored after `Publish` and `BuildDockerContainer`.
  - `InsertCounterCode` already did nothing: its placeholder `<!-- Web Counter here -->` is not in `index.html`, and git history shows it never was. This change doesn't affect it. The service-worker unregistration script in index.html is unchanged.
- **Measured (Release publish, `dotnet BlazorWasmRegex.Server.dll` on http://127.0.0.1:5099):**
  - `/`, `/about` and `/index.html` return 200 `text/html` with `Cache-Control: no-cache`. `/index.html` with `Accept-Encoding: br` is served br-encoded (941 B instead of 2,946 B).
  - Fingerprinted framework files return `Cache-Control: max-age=31536000, immutable`, for example `dotnet.8blsku27ul.js` and `dotnet.native.mjledzavap.wasm`.
  - Before this change every `_framework` file was `no-cache` and must revalidate on each load. CSS and favicon had no `Cache-Control` and no compression.
  - `dotnet.native.*.wasm` is `application/wasm` with br at 1,003,700 B, gzip at 1,253,053 B and identity at 3,150,989 B.
  - CSS (`css/app.css`) is now br-compressed.
  - Unknown file paths (e.g. `/nonexistent.js`) still return 404.
  - The 69 requests of a cold load of `/about` (captured in Chrome) total 10.77 MB identity, 4.21 MB gzip and 3.38 MB br. The pre-change build measured about 3.52 MB with br. Most of the saving comes from the CSS now being compressed. The bigger win is repeat visits: the framework files are now cached as immutable instead of revalidated.
  - In Chrome the published app booted on the `/about` deep link with no console errors. Every `_framework` request used a fingerprinted URL except `blazor.webassembly.js`.
- **Verified:**
  - `dotnet build BlazorWasmRegex2.sln -c Release`: 0 warnings, 0 errors (baseline 0).
  - `dotnet test tests/RegexService.Tests`: 40/40 pass.
  - `dotnet run --project BlazorWasmRegex/Server` (Development profile): `/` and `/about` return 200, and the `_framework/blazor.webassembly.ldl9n57ejg.js` URL from the served index.html returns 200.
  - `./build.sh -Target Publish` succeeds. The published index.html has a real importmap and a `dotnet.<hash>.js` preload.
  - `./build.sh -Target BuildDockerContainer` succeeds. The container on 127.0.0.1:18093 served `/` and `/about` (200, `no-cache`), a filled importmap, the fingerprinted `dotnet.*.js` (200), and `.wasm` as `application/wasm`, br, immutable. The container was removed afterwards.
  - `Dockerfile-Arm` was not built.

### Step 4: more RegexOptions in the UI

- **UI (`RegexTester.razor`):** an "Advanced" toggle under Ignore Case / Multiline / Singleline shows a second row of checkboxes: Any NewLine, NonBacktracking, Explicit Capture, Ignore Pattern Whitespace, Right To Left, Culture Invariant and ECMAScript. Each option has a short `title` tooltip. The toggle shows how many advanced options are on, and the section opens on load when any of them is saved as on, so active options are never hidden.
- **New Shared service `IRegexOptionsService` / `RegexOptionsService`** (registered in `Client/Program.cs`), with `RegexOptionsData` moved to `Shared/Models`:
  - `ToRegexOptions` maps the flags (still never adds `Compiled`).
  - `Validate` returns friendly messages for combinations the engine rejects.
  - `TryCreateRegex` validates, then constructs the regex with the caller's timeout (the component still passes its 2 s `RegexTimeout`). It turns `RegexParseException`, `NotSupportedException` (NonBacktracking with an unsupported construct) and `ArgumentException` into an error result. Before this, `NotSupportedException` was not caught by the component, because it is not an `ArgumentException`.
  - `Serialize` / `Deserialize` handle the `REGEX_OPTIONS` storage formats.
- **Option rules, checked empirically on .NET 11.0.100** (scratch console app, now encoded as tests that compare `Validate` with the real `Regex` constructor) and matching the "Regular expression options" docs:
  - ECMAScript only combines with IgnoreCase, Multiline and CultureInvariant. CultureInvariant is accepted even though older docs list only IgnoreCase and Multiline.
  - NonBacktracking can't be combined with RightToLeft or ECMAScript.
  - AnyNewLine can't be combined with NonBacktracking or ECMAScript (with or without Multiline). It does combine with RightToLeft, Singleline, Multiline, ExplicitCapture, IgnorePatternWhitespace, CultureInvariant and IgnoreCase.
  - All of these throw `ArgumentOutOfRangeException` (param `options`). NonBacktracking with backreferences, lookarounds, atomic groups, conditionals or `\G` throws `NotSupportedException` at construction.
  - With Multiline, `^.+$` over `a\u2028b\r\nc` gives `a\u2028b\r`, `c` without AnyNewLine and `a`, `b`, `c` with it.
- **WASM detail:** Blazor WASM trims resource strings (`UseSystemResourceKeys`), so in the browser the `NotSupportedException` message is only resource keys (`NotSupported_NonBacktrackingConflictingExpression, ExpressionDescription_Backreference`). The service extracts the construct from either form, so the user sees "NonBacktracking does not support this pattern: it contains backreference."
- **Storage compatibility:** every new `RegexOptionsData` field has a `false` default, so the old 3-field JSON still deserializes. The legacy pipe-delimited format is still read. The JSON property names are unchanged. Change detection (`prevRegexConfig`) is now `(RegexText, RegexOptionsData)`, so toggling any option recompiles.
- **Verified:**
  - `dotnet build BlazorWasmRegex2.sln -c Release`: 0 warnings, 0 errors (baseline 0).
  - `dotnet test tests/RegexService.Tests`: 86/86 pass (46 new). The new tests cover option mapping, the combination matrix against the real constructor, NonBacktracking unsupported constructs, parse errors, AnyNewLine behaviour, old 3-field JSON, the new JSON round trip, legacy pipe strings and full/trimmed exception messages.
  - The app ran in Chrome (`dotnet run`, Development). Old 3-field JSON seeded into `localStorage` loaded with Ignore Case + Multiline checked. NonBacktracking with `(a)\1`, NonBacktracking + Any NewLine, and ECMAScript + Any NewLine each showed a friendly message with no crash. Any NewLine alone matched and was saved in the new JSON, and after a reload it came back checked with the Advanced section open.

## Known issues and follow-ups

- **Harmless NUKE warning:** an "Exception was suppressed … NuGet.Frameworks 7.11.0.0" message appears during NUKE runs. Nuke's bundled NuGet assemblies are older than the .NET 11 SDK's MSBuild; Nuke catches the exception and the build succeeds. It should go away when Nuke ships a .NET 11-aware release.
