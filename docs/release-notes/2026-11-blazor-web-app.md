# Release notes: Blazor Web App with build-time prerendering

**Date:** 2026-11-11
**Change:** The app moved from a standalone Blazor WebAssembly client (an empty `index.html` plus a spinner) to a Blazor Web App. Pages are rendered to static HTML when the Docker image is built, and only the regex tester is an interactive WebAssembly component. This branch also includes the earlier search-indexing fixes and some improvements to the tester.

## Why

Search engines and link previews got an empty page: the HTML only contained a loading spinner until the .NET runtime started. Google renders JavaScript, but with a delay and a time budget; Bing, DuckDuckGo and social previews mostly don't. Server-side rendering per request isn't an option either, because the production host is an Orange Pi Zero with 256 MB RAM.

So the pages are server-rendered **once, at build time**, on the build machine. The production server serves the saved HTML from memory and never renders a Razor component.

## What changed

### Search indexing (first commit)

- Descriptive titles, meta descriptions, canonical links and Open Graph/Twitter tags on every page.
- `robots.txt` and `sitemap.xml`.
- Unknown paths return 404 instead of 200 with the home page.
- About page: "256GB" corrected to "256 MB".

### Blazor Web App

- **Server project** is now the Blazor Web App host. `Components/` holds `App.razor`, `Routes.razor`, the layout and nav menu (moved from Client), and the Home, About and NotFound pages, all statically rendered. `PageMeta.razor` renders each page's title, description, canonical link and OG tags.
- **Client project** only holds the interactive part (`RegexTester`) and its services. `index.html`, `App.razor` and the root component registration are gone.
- **`Program.cs` modes:**
  - *live* (`dotnet run`, no `prerendered/` folder): pages are rendered per request.
  - *prerender* (`--PrerenderTo <dir>`): `Prerenderer` starts the app on a free local port, fetches `/`, `/about` and `/not-found`, and saves them. It also saves `_framework/resource-collection[.fingerprint].js`, the asset list the WebAssembly runtime loads. `MapRazorComponents` generates that file per request, and the import map pins it by SRI hash, so it's saved byte for byte. The step fails if a page doesn't return 200, isn't a full HTML document, or contains the temporary server's address.
  - *static* (`prerendered/` exists under the content root): `StaticPages` loads the files at startup and serves them from memory, with Brotli/gzip variants, ETags (304 on revalidation) and `Cache-Control: no-cache` for pages. Fingerprinted framework files get `immutable` caching. Unknown paths get the not-found page with a 404 status. Pages send `blazor-enhanced-nav: allow`, so moving between pages patches the DOM instead of reloading, and the WebAssembly runtime stays loaded.
- **Dockerfiles:** after `dotnet publish`, `RUN dotnet BlazorWasmRegex.Server.dll --PrerenderTo /app/publish/prerendered`. This runs on the build platform, including in `Dockerfile-Arm`, which already builds on amd64.
- **Service worker removed.** `index.html` unregistered every worker on load, so it never did anything; its "serve cached `index.html` for every navigation" logic would now be wrong. `js/app.js` still unregisters any worker left in a browser. The Docker stamp step for `service-worker.published.js` is gone, as is the `index.html` hosted-publish workaround and its publish guard.
- **Nav menu:** static, so the mobile toggle is a CSS-only checkbox. The HW Monitor link is now absolute (`https://monitor.dotnet-regex.com/`), because pages are rendered at build time and can't use the request URL. The brand reads ".NET Regex Tester", and "Home" is now "Regex Tester".

### Main page

- An `<h1>` and a short description above the tester.
- **.NET regex quick reference** below the tester: character classes, anchors, quantifiers, groups and lookarounds, inline options, substitutions and escapes. It's useful for users and gives search engines real content.
- The tester is **prerendered disabled**, with "Loading the .NET regex engine…" in the status area, and enables itself when WebAssembly starts. Without this, text typed before the runtime loaded would be lost when the interactive component replaced the static copy.
- Saved state is loaded from `localStorage` only once the component is interactive (`RendererInfo.IsInteractive`). There is no JS interop while prerendering.
- **Ctrl+Enter** (Cmd+Enter) runs the test. The pattern, replacement and tests inputs update as you type, so the shortcut sees the current text.
- Monospace pattern, replacement and test inputs, with spellcheck and autocapitalize turned off.
- Labels are linked to their inputs. `<p>` wrappers that contained `<div>`s (invalid HTML, which browsers restructure) are now `<div>`s.

## How it was verified

- `dotnet build BlazorWasmRegex2.sln` succeeds, and `dotnet test tests/RegexService.Tests` passes (104 tests).
- **Live mode** (`dotnet run`): `/`, `/about` and `/not-found` return 200. Unknown paths return 404 with the not-found page. Assets are fingerprinted.
- **Docker** (`Dockerfile`, x64, `VERSION=2026.11.11.2 SOURCE_REV=test123`): the prerender step saves the three pages and two framework files, and the container serves:

  | Path | Result |
  |---|---|
  | `/`, `/about` | 200, Brotli |
  | `/nonexistent-page`, `/foo/bar` | 404, not-found page |
  | `/_framework/resource-collection.<fp>.js` and `.js.gz` | 200, `immutable` |
  | conditional `GET /` | 304 |

  The nav shows `v 2026.11.11.2+test123`.
- **Memory:** after a few requests, 30.1 MB versus 28.4 MB for the current production `x64` image.
- **Browser (Chrome) against the container and a local published build:**
  - The form is visible and disabled before WebAssembly starts. It became interactive about 1.2 s after load with a warm cache, and 3.9 s on a cold one.
  - The samples, highlighting, Ctrl+Enter and `localStorage` save/restore work.
  - Home → About → Home happens without a document reload. The tester is interactive again within about 50 ms and restores the saved pattern.
  - Each page has exactly one title, description and canonical link.
  - At phone width the menu opens and closes with no horizontal scroll.
- The ARM image (`Dockerfile-Arm`) was **not** built locally. Its prerender step is identical and runs on the amd64 build platform; CI builds it.

## Known issues / follow-ups

- The bundled Bootstrap CSS is 4.3.1, while BlazorStrap's components are V5, so some Bootstrap 5 classes (e.g. the samples `form-select`) are unstyled. This predates this change.
- Pages carry a random `prerenderId` and fingerprinted asset URLs from build time, so every image build produces new page HTML (and new ETags) even when nothing changed.
- `StaticPages.Pages` and `sitemap.xml` must be kept in sync with the `@page` routes by hand.
- The server still logs DataProtection warnings at startup (Razor Pages' antiforgery for `/Error`), as it did before.
