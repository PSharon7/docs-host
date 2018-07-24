using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Document.Hosting.RestClient;
using Newtonsoft.Json.Linq;

namespace docs.host
{
    public static class Publish
    {

        private static readonly DocumentHostingServiceClient s_dhsClient =
            new DocumentHostingServiceClient(new Uri(Config.Get("dhs_baseuri")), Config.Get("dhs_clientname"), Config.Get("dhs_apiaccesskey"));

        public static async Task Migrate(string basePath, string branch, string locale, int top = 3)
        {
            var depots = await s_dhsClient.GetAllDepotsBySiteBasePath("docs", basePath, null, CancellationToken.None);
            var topDepots = depots.OrderBy(d => d.Priority).Take(top);
            var client = new HttpClient();
            var activeEtag = DateTime.UtcNow.ToString("o");

            await ParallelUtility.ParallelForEach(topDepots, async topDepot =>
            {
                var documents = await s_dhsClient.GetAllDocuments(topDepot.DepotName, branch, locale, false, null, CancellationToken.None);
                await ParallelUtility.ParallelForEach(documents, async document =>
                {
                    var pageDocs = new ConcurrentBag<Document>();
                    using (var request = new HttpRequestMessage(HttpMethod.Get, document.ContentUri))
                    using (Stream contentStream = await (await client.SendAsync(request)).Content.ReadAsStreamAsync())
                    {
                        var (pageUrl, pageHash) = await Writer.UploadPage(contentStream, document.CombinedMetadata["content_type"].ToString());
                        var pageDoc = new Document
                        {
                            Docset = topDepot.DepotName,
                            Url = $"docs.microsoft.com/{basePath}/{document.AssetId}",
                            Locale = locale,
                            Branch = branch,
                            Monikers = (document.CombinedMetadata["monikers"] as JArray)?.ToObject<List<string>>(),
                            ActiveEtag = activeEtag,
                            PageHash = pageHash,
                            PageUrl = pageUrl,
                            PageType = document.CombinedMetadata["page_type"].ToString(),
                            Title = document.CombinedMetadata["title"].ToString(),
                            Layout = document.CombinedMetadata["layout"].ToString(),
                            IsDynamicRendering = (bool)document.CombinedMetadata["is_dynamic_rendering"],
                            ContentType = document.CombinedMetadata["content_type"].ToString()
                        };
                        pageDocs.Add(pageDoc);
                    }

                    await Writer.UploadDocuments(pageDocs.ToList(), activeEtag, (done, total) =>
                    {
                        var percent = ((int)(100 * Math.Min(1.0, done / Math.Max(1.0, total)))).ToString();
                        Console.WriteLine($"{topDepot.DepotName}: {percent.PadLeft(3)}% {done}/{total}");
                    });
                }, 2000, 1000);
            }, 10, 5);
        }
    }
}
