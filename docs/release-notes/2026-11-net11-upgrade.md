# Release notes: upgrade to .NET 11

**Date:** 2026-11-10
**Change:** .NET 10 (LTS) → .NET 11 (STS) for all projects, the build, CI and container images.

## What the upgrade gives us

### For users of the regex tester

The app runs the real .NET regex engine in the browser, so the runtime version *is* the product: patterns are now evaluated with .NET 11 semantics.

- **Regex correctness fixes in .NET 11:**
  - The `NonBacktracking` engine no longer takes super-linear time on certain nested-loop patterns, and returns correct results in cases that previously diverged from the backtracking engine.
  - The regex compiler and source generator handle `resumeAt` correctly when a conditional sits inside a loop body.
- **New `RegexOptions.AnyNewLine`:** `^`, `$` and `.` treat all Unicode newlines (`\r\n`, `\n`, `\u0085`, `\u2028`, `\u2029`) as line terminators. It isn't in the UI yet; it would be a cheap addition next to the Multiline/Singleline checkboxes (see follow-ups).

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

## Known issues and follow-ups

- **Harmless NUKE warning:** an "Exception was suppressed … NuGet.Frameworks 7.11.0.0" message appears during NUKE runs. Nuke's bundled NuGet assemblies are older than the .NET 11 SDK's MSBuild; Nuke catches the exception and the build succeeds. It should go away when Nuke ships a .NET 11-aware release.
- **Possible feature:** expose `RegexOptions.AnyNewLine` as an option checkbox.
