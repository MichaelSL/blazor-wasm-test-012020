# Release notes: host on Cloudflare (Workers static assets)

**Date:** 2026-11-12
**Change:** dotnet-regex.com is now served from Cloudflare as a static site, deployed by GitHub Actions. The self-hosted ARM board is no longer in the serving path.

## Why

The self-hosted Orange Pi went down during a system update and couldn't be recovered remotely. Since the build-time prerendering change, the site is only static files, so it needs no server. Cloudflare's free plan serves static assets with no request or bandwidth limit, adds Brotli/gzip compression at the edge, and puts the site on a global CDN.

## What changed

- **`deploy/cloudflare/export_static.py`:** turns `dotnet publish` output plus the prerender step into plain files:
  - every URL in `BlazorWasmRegex.Server.staticwebassets.endpoints.json`, including fingerprinted ones such as `css/app.<hash>.css`, is written as a real file from its uncompressed source;
  - the prerendered `index.html`, `about.html` and `404.html` are added, along with the generated `_framework/resource-collection*.js`.
  - The import map also lists `resource-collection.<hash>.js.gz`, which the server answers with the gzip-encoded script. The export writes the plain script at that URL, and `_headers` serves it as JavaScript. The browser has not been seen requesting it.
- **`deploy/cloudflare/wrangler.jsonc`:** a static-only Worker named `dotnet-regex`, with no script.
  - `not_found_handling: 404-page` serves `404.html` with status 404.
  - `html_handling: auto-trailing-slash` serves `/about` from `about.html`.
  - `dotnet-regex.com` and `www.dotnet-regex.com` are custom domains.
- **`deploy/cloudflare/_headers`:** adds `X-Content-Type-Options: nosniff` and `Referrer-Policy`, and sets the content type for the `.gz` script.
- **`.github/workflows/deploy-cloudflare.yml`:**
  - On pushes to `main` it runs test, publish, prerender, export, then `wrangler deploy`.
  - Pull requests run the export without deploying.
  - Secrets: `CLOUDFLARE_API_TOKEN` (an account-owned token with Workers Scripts Write and Account Settings Read, plus Zone Read, Workers Routes Write and DNS Write on the dotnet-regex.com zone only) and `CLOUDFLARE_ACCOUNT_ID`.
- **DNS:**
  - The zone moved to Cloudflare; the registrar's nameservers now point at Cloudflare.
  - `monitor.dotnet-regex.com` keeps its DNS-only CNAME to the home network's dynamic DNS name.
  - The apex and `www` are Worker custom domains, so Cloudflare manages their records and certificates.
- **Docs:** the About page, README and AGENTS.md describe the new hosting. The Docker images are still built for self-hosting.

## How it was verified

- **Local export test:** ran the export on a local publish + prerender and served it with `wrangler dev` (workerd), checked with curl:
  - `/` and `/about` return 200; `/about/` redirects to `/about`; an unknown path returns 404 with the 404 page;
  - fingerprinted CSS is served; the `.gz` script is served as `text/javascript`.
  - In Chrome the tester loads and matches, with no failed requests and no console errors.
- **First deploy (local, using the deploy token):**
  - checked on `https://dotnet-regex.michael-samorokov.workers.dev/`: the tester loads and works with no console errors;
  - responses are compressed (about 3.7 MB transferred; the main `.wasm` is 1.2 MB instead of 3.1 MB).

## Known issues / follow-ups

- The **HW Monitor** link (to `monitor.dotnet-regex.com` on the home network) was removed afterwards: the Pi is no longer used.
- Cache headers are Cloudflare's defaults: `max-age=0, must-revalidate` with ETags. Fingerprinted files could be marked immutable, but `_headers` patterns can't tell fingerprinted from plain names, and its rule limit is 100.
- The Docker image workflow still runs on every push. It could be dropped if self-hosting is abandoned.
