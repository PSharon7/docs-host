using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Document.Hosting;
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
            Console.WriteLine($"Get depots for {basePath}");
            var depots = await s_dhsClient.GetAllDepotsBySiteBasePath("Docs", basePath, null, CancellationToken.None);
            var topDepots = depots.OrderBy(d => d.Priority).Take(top);
            var client = new HttpClient();
            var activeEtag = DateTime.UtcNow.ToString("o");

            await ParallelUtility.ParallelForEach(topDepots, async topDepot =>
            {
                Console.WriteLine($"Load documents for {topDepot.DepotName}");
                var documents = await s_dhsClient.GetAllDocuments(topDepot.DepotName, locale, branch, false, null, CancellationToken.None);

                Console.WriteLine($"Convert {documents.Count} documents for {topDepot.DepotName}");
                var pageDocs = new ConcurrentBag<Document>();
                await ParallelUtility.ParallelForEach(documents, async document =>
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, document.ContentUri))
                    using (Stream contentStream = await (await client.SendAsync(request)).Content.ReadAsStreamAsync())
                    {
                        var (pageUrl, pageHash) = await Writer.UploadPage(contentStream, document.CombinedMetadata.GetValueOrDefault<string>("content_type"));
                        var pageDoc = new Document
                        {
                            Docset = topDepot.DepotName,
                            Url = $"{basePath}{document.AssetId}",
                            Locale = locale,
                            Branch = branch,
                            Monikers = document.CombinedMetadata.GetValueOrDefault<JArray>("monikers")?.ToObject<List<string>>(),
                            ActiveEtag = activeEtag,
                            PageHash = pageHash,
                            PageUrl = pageUrl,
                            PageType = document.CombinedMetadata.GetValueOrDefault<string>("page_type"),
                            Title = document.CombinedMetadata.GetValueOrDefault<string>("title"),
                            Layout = document.CombinedMetadata.GetValueOrDefault<string>("layout"),
                            IsDynamicRendering = document.CombinedMetadata.GetValueOrDefault<bool>("is_dynamic_rendering"),
                            ContentType = document.CombinedMetadata.GetValueOrDefault<string>("content_type")
                        };
                        pageDocs.Add(pageDoc);
                    }
                },
                2000,
                1000,
                (done, total) =>
                {
                    var percent = ((int)(100 * Math.Min(1.0, done / Math.Max(1.0, total)))).ToString();
                    Console.WriteLine($"Uploading Page Content for {topDepot.DepotName}: {percent.PadLeft(3)}% {done}/{total}");
                });

                await Writer.UploadDocuments(pageDocs.ToList(), activeEtag, (done, total) =>
                {
                    var percent = ((int)(100 * Math.Min(1.0, done / Math.Max(1.0, total)))).ToString();
                    Console.WriteLine($"Uploading Page Document for {topDepot.DepotName}: {percent.PadLeft(3)}% {done}/{total}");
                });
            }, 10, 5);
        }
    }
}
