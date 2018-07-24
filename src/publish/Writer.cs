using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace docs.host
{
    public static class Writer
    {
        public static async Task<(string pageUrl, string pageHash)> UploadPage(Stream pageStream, string contentType)
        {
            string pageUrl;
            string hash = HashUtility.GetSha1HashString(pageStream);
            
            if (contentType == "application/json" || contentType == "text/html" || contentType == "text/plain")
            {
                Page page = await CosmosDBAccessor<Page>.GetAsync(hash);

                if (page is null)
                {
                    StreamReader sr = new StreamReader(pageStream);
                    page = new Page()
                    {
                        id = hash,
                        Hash = hash,
                        Content = sr.ReadToEnd(),
                    };

                    sr.Close();
                    await CosmosDBAccessor<Page>.UpsertAsync(page);
                }

                pageUrl = Config.Get("cosmos_endpoint") + CosmosDBAccessor<Page>.GetDocumentUri(hash).ToString();
            }
            else
            {
                var blob = BlobAccessor.cloudBlobContainer.GetBlockBlobReference(hash);
                bool blobExist = await blob.ExistsAsync();

                if (!blobExist)
                {
                    await blob.UploadFromStreamAsync(pageStream);
                }

                pageUrl = blob.Uri.AbsoluteUri;
            }

            pageStream.Close();
            return (pageUrl, hash);
        }

        public static async Task UploadDocuments(List<Document> documents, string activeEtag, Action<int, int> progress)
        {
            if (documents == null || !documents.Any())
            {
                return;
            }

            // upload documents
            await ParallelUtility.ParallelForEach(documents, document =>
            {
                return CosmosDBAccessor<Document>.UpsertAsync(document);
            }, 2000, 1000, progress);

            // switch active etag
            var doc = documents.First();
            var active = new Active
            {
                ActiveEtag = activeEtag,
                IsActive = false,
                Branch = doc.Branch,
                Locale = doc.Locale,
                Docset = doc.Docset,
                id = HashUtility.GetSha1HashString($"{doc.Docset}|{doc.Branch}|{doc.Locale}")
            };

            await CosmosDBAccessor<Active>.UpsertAsync(active);
        }
    }
}
