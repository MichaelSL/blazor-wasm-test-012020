# Release notes: replace NUKE with GitHub Actions

**Date:** 2026-11-10
**Change:** Removed the NUKE build project. CI now calls `dotnet` and Docker directly from GitHub Actions, and a new workflow cleans up old images.

## Why

NUKE is effectively unmaintained. The author declined to hand the repo to a successor, and `Nuke.Build 10.1.0` (Dec 2025) is the last release. Nothing was broken, but the build was mostly thin wrappers around `dotnet` and `docker`, so a build tool wasn't worth keeping.

## What changed

- **Removed:** `build/`, `.nuke/`, `build.sh`, `build.ps1`, `build.cmd`, the `_build` project in `BlazorWasmRegex2.sln`, and `.github/workflows-main.yml`. That workflow sat outside `workflows/`, so it never ran, which meant CI wasn't running the tests.
- **Versioning:** `Directory.build.props.template` became a committed `Directory.Build.props` with a default `Version` of `0.0.0.1`. The Dockerfiles take `VERSION` and `SOURCE_REV` build args and pass them as `-p:Version` / `-p:SourceRevisionId`. CI sets `VERSION=yyyy.M.d.<run number>` (UTC) and `SOURCE_REV=<short sha>`. The build no longer rewrites tracked files, so there is nothing to restore or keep out of commits.
- **Version display:** the nav menu shows the informational version with a short hash, e.g. `v 2026.11.10.57+a1b2c3d` (`AppVersionFormatter` in Shared, with tests). Local builds show `0.0.0.1+<sha>` from SourceLink.
- **Service worker stamp:** the timestamp is replaced by the commit. The Docker build stage appends `// build: <sha>` to its copy of `service-worker.published.js`, so a rebuild of the same commit produces the same worker.
- **`create-images.yml`:**
  - A `test` job runs `dotnet test tests/RegexService.Tests`.
  - The `images` job builds x64 and ARM (`linux/arm/v7`) with `docker/build-push-action`, using `no-cache` and `provenance: false`. Tags are unchanged: `x64` / `arm`, with `-<branch>` off master.
  - Pull request runs build but no longer push. Images for a PR come from its `feature/**` branch push.
- **`cleanup-images.yml` (new):** only the latest image per tag is kept.
  - When a branch is deleted, its `x64-<branch>` / `arm-<branch>` tags are removed.
  - Weekly (Sunday 03:17 UTC), untagged versions older than 7 days and branch tags older than 30 days are removed.
  - Manual runs default to dry-run.
  - The `x64` and `arm` tags are never deleted.
  - It uses `dataaxiom/ghcr-cleanup-action` pinned to v1.2.2's commit. `actions/delete-package-versions` can't select by tag.
- **Dropped:** counter-code injection. Its placeholder was removed from `index.html` on 2022-11-11 (`eafa1a1`, "removed yandex counter"), and CI stopped passing the code in January 2023, so the target did nothing.

## How it was verified

- `dotnet build BlazorWasmRegex2.sln` succeeds. `dotnet test tests/RegexService.Tests` passes (104 tests).
- Ran `docker build` (x64) with `VERSION=2026.11.10.99 SOURCE_REV=abc1234`:
  - the published `service-worker.js` ends with `// build: abc1234`
  - the client `.wasm` and the server DLL carry `2026.11.10.99` and `2026.11.10.99+abc1234`.
- `docker buildx build --platform linux/arm/v7` with `Dockerfile-Arm` succeeds and produces an `arm` image.

## Known issues / follow-ups

- `index.html` unregisters every service worker on load, in production too, so the stamp has no effect until that is fixed (tracked separately).
- The branch-delete cleanup runs from the workflow file on the default branch, so it only takes effect after this change is merged.
- Registry storage has no per-repo quota setting. Retention relies on the cleanup workflow.
