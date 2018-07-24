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
using Microsoft.Document.Hosting.RestService.Contract;
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
            var activeEtag = Guid.NewGuid().ToString();

            await ParallelUtility.ParallelForEach(topDepots, async topDepot =>
            {
                var continueAt = string.Empty;
                var documents = new List<GetDocumentResponse>();
                var i = 1;
                do
                {
                    Console.WriteLine($"Load {1000 * i++} documents for {topDepot.DepotName}");
                    var documentsResponse = await s_dhsClient.GetDocumentsPaginated(topDepot.DepotName, locale, branch, false, continueAt, null, 1000, CancellationToken.None);
                    documents.AddRange(documentsResponse.Documents);
                    continueAt = documentsResponse.ContinueAt;
                } while (!string.IsNullOrEmpty(continueAt));

                Console.WriteLine($"Convert {documents.Count} documents for {topDepot.DepotName}");
                var pageDocs = new ConcurrentBag<Document>();
                await ParallelUtility.ParallelForEach(documents, async document =>
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, document.ContentUri))
                    using (Stream contentStream = await (await client.SendAsync(request)).Content.ReadAsStreamAsync())
                    {
                        var (pageUrl, pageHash) = await Writer.UploadPage(contentStream, document.CombinedMetadata.GetValueOrDefault<bool>("is_dynamic_rendering"), document.CombinedMetadata.GetValueOrDefault<string>("content_type"));
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
                400,
                200,
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
