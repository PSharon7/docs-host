using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;

namespace docs.host
{
    [ApiController]
    public class DocumentController : ControllerBase
    {
        private const string Host = "docs.microsoft.com";

        private static readonly char[] PathSeparators = new[] { '/' };

        [HttpGet("{*path}")]
        public async Task<ActionResult> GetAsync(string path, [FromQuery]string branch, [FromQuery]string view)
        {
            var pathAndLocale = ResolvePathAndLocale(path);

            // not locale input, redirected to en-us
            if (pathAndLocale.Item2 == null)
            {
                return RedirectPermanent($"/en-us/{path}{HttpContext.Request.QueryString.ToString()}");
            }

            Document doc = await Reader.QueryDocument($"{Host}{pathAndLocale.Item1}", branch, pathAndLocale.Item2, view);
            if (doc == null)
            {
                return NotFound();
            }

            // moniker change
            string finalMoniker = doc.Monikers.FirstOrDefault();
            if (finalMoniker != null && !string.Equals(finalMoniker, view, StringComparison.OrdinalIgnoreCase))
            {
                var query = QueryHelpers.ParseQuery(HttpContext.Request.QueryString.ToString());
                query["view"] = finalMoniker;
                if (!string.IsNullOrWhiteSpace(view))
                {
                    query["viewFallbackFrom"] = view;
                }
                var updatedQuery = QueryString.Create(query);
                return RedirectPermanent($"/{path}{updatedQuery.ToString()}");
            }

            return Ok(doc.IsDynamicRendering ? (object)await Reader.GetPageContent(doc.PageUrl) : doc);
        }

        private Tuple<string, string> ResolvePathAndLocale(string path)
        {
            Uri uri = new Uri($"https://{Host}/{path}", UriKind.Absolute);
            string finalPath = uri.AbsolutePath;
            finalPath = (finalPath ?? "/index").ToLowerInvariant();
            finalPath = LocaleHelper.GetPathWithoutLocale(finalPath, out string locale);
            finalPath = finalPath.StartsWith("/") ? finalPath : "/" + finalPath;
            finalPath = finalPath.EndsWith("/") ? finalPath + "index" : finalPath;
            return Tuple.Create(finalPath, locale);
        }
    }
}
