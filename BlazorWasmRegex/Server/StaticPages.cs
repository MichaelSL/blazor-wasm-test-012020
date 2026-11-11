#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace BlazorWasmRegex.Server;

/// <summary>
/// The site's pages, rendered to HTML once at image build time (<see cref="Prerenderer"/>) and served
/// from memory, so the production server (a tiny ARM board) never renders Razor components.
/// </summary>
public static partial class StaticPages
{
    /// <summary>Folder under the content root that holds the prerendered output.</summary>
    public const string DirectoryName = "prerendered";

    /// <summary>Route of the not-found page; its HTML is served with 404 for every unknown path.</summary>
    public const string NotFoundRoute = "/not-found";
    public const string NotFoundFile = "404.html";

    /// <summary>Routable pages. Keep in sync with the @page directives and sitemap.xml.</summary>
    public static readonly (string Route, string File)[] Pages =
    [
        ("/", "index.html"),
        ("/about", "about.html"),
    ];

    /// <summary>
    /// Subfolder for files that MapRazorComponents generates per request rather than serving from
    /// wwwroot: <c>_framework/resource-collection[.fingerprint].js</c>, the asset list the
    /// WebAssembly runtime loads. Pages reference them (with SRI hashes) in their import map.
    /// </summary>
    public const string FrameworkDirectoryName = "_framework";

    /// <summary>Generated framework files referenced by a page, as site-relative paths without the ".gz" variants.</summary>
    public static string[] FindGeneratedFrameworkFiles(string html) =>
        GeneratedFrameworkFileRegex().Matches(html).Select(m => m.Value).Distinct().ToArray();

    [GeneratedRegex(@"_framework/resource-collection(\.[A-Za-z0-9]+)?\.js(?!\.gz)")]
    private static partial Regex GeneratedFrameworkFileRegex();

    /// <summary>Maps every page, the generated framework files and a 404 fallback. Throws if a file is missing.</summary>
    public static void Map(WebApplication app, string directory)
    {
        foreach (var (route, file) in Pages)
        {
            var page = StaticFile.LoadHtml(Path.Combine(directory, file));
            app.MapMethods(route, [HttpMethods.Get, HttpMethods.Head], ctx => page.WriteAsync(ctx, StatusCodes.Status200OK));
        }

        var frameworkDirectory = Path.Combine(directory, FrameworkDirectoryName);
        if (Directory.Exists(frameworkDirectory))
        {
            foreach (var path in Directory.GetFiles(frameworkDirectory, "*.js"))
            {
                var name = Path.GetFileName(path);
                var script = StaticFile.LoadScript(path, fingerprinted: name != "resource-collection.js");
                var route = $"/{FrameworkDirectoryName}/{name}";
                app.MapMethods(route, [HttpMethods.Get, HttpMethods.Head], ctx => script.WriteAsync(ctx, StatusCodes.Status200OK));
                // The import map also lists a precompressed ".gz" URL that is served gzip-encoded.
                app.MapMethods(route + ".gz", [HttpMethods.Get, HttpMethods.Head], ctx => script.WriteAsync(ctx, StatusCodes.Status200OK, forceGzip: true));
            }
        }

        var notFound = StaticFile.LoadHtml(Path.Combine(directory, NotFoundFile));
        app.MapFallback(ctx => notFound.WriteAsync(ctx, StatusCodes.Status404NotFound));
    }

    private sealed class StaticFile
    {
        private readonly byte[] _identity;
        private readonly byte[] _brotli;
        private readonly byte[] _gzip;
        private readonly string _etag;
        private readonly string _contentType;
        private readonly string _cacheControl;
        private readonly bool _isPage;

        private StaticFile(byte[] identity, string contentType, string cacheControl, bool isPage)
        {
            _identity = identity;
            _brotli = Compress(identity, s => new BrotliStream(s, CompressionLevel.SmallestSize));
            _gzip = Compress(identity, s => new GZipStream(s, CompressionLevel.SmallestSize));
            _etag = $"\"{Convert.ToHexString(SHA256.HashData(identity), 0, 8)}\"";
            _contentType = contentType;
            _cacheControl = cacheControl;
            _isPage = isPage;
        }

        // Pages reference fingerprinted assets, so browsers must revalidate them on every load.
        public static StaticFile LoadHtml(string path) =>
            new(Read(path), "text/html; charset=utf-8", "no-cache", isPage: true);

        public static StaticFile LoadScript(string path, bool fingerprinted) =>
            new(Read(path), "text/javascript", fingerprinted ? "max-age=31536000, immutable" : "no-cache", isPage: false);

        private static byte[] Read(string path) =>
            File.Exists(path)
                ? File.ReadAllBytes(path)
                : throw new FileNotFoundException($"Prerendered file '{path}' is missing. Run the server with --PrerenderTo to generate it.", path);

        public async Task WriteAsync(HttpContext ctx, int statusCode, bool forceGzip = false)
        {
            var request = ctx.Request;
            var response = ctx.Response;
            var headers = response.Headers;

            headers.CacheControl = _cacheControl;
            headers.ETag = _etag;
            headers.Vary = HeaderNames.AcceptEncoding;
            if (_isPage)
            {
                // Lets blazor.web.js use enhanced navigation (fetch + DOM patch) between pages instead
                // of a full reload, which would restart the .NET WebAssembly runtime.
                headers["blazor-enhanced-nav"] = "allow";
            }

            if (statusCode == StatusCodes.Status200OK &&
                request.Headers.IfNoneMatch.Any(v => v is not null && (v == "*" || v.Split(',').Any(t => t.Trim() == _etag))))
            {
                response.StatusCode = StatusCodes.Status304NotModified;
                return;
            }

            var acceptEncoding = request.Headers.AcceptEncoding.ToString();
            var (body, encoding) =
                forceGzip ? (_gzip, "gzip")
                : acceptEncoding.Contains("br", StringComparison.OrdinalIgnoreCase) ? (_brotli, "br")
                : acceptEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase) ? (_gzip, "gzip")
                : (_identity, null);

            response.StatusCode = statusCode;
            response.ContentType = _contentType;
            response.ContentLength = body.Length;
            if (encoding is not null)
            {
                headers.ContentEncoding = encoding;
            }

            if (!HttpMethods.IsHead(request.Method))
            {
                await response.Body.WriteAsync(body, ctx.RequestAborted);
            }
        }

        private static byte[] Compress(byte[] data, Func<Stream, Stream> createCompressor)
        {
            using var output = new MemoryStream();
            using (var compressor = createCompressor(output))
            {
                compressor.Write(data);
            }
            return output.ToArray();
        }
    }
}
