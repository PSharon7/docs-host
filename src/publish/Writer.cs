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

            /* img or pdf*/
            if (contentType == "image/jpeg" || contentType == "image/png" || contentType == "application/pdf")
            {
                var blob = BlobAccessor.cloudBlobContainer.GetBlockBlobReference(hash);
                bool blobExist = await blob.ExistsAsync();

                if (!blobExist)
                {
                    await blob.UploadFromStreamAsync(pageStream);
                }

                pageUrl = blob.Uri.AbsoluteUri;
            }
            else
            {
                ICollection<Page> pageExist = (ICollection<Page>)CosmosDBAccessor<Page>.QueryAsync(p => p.Hash == hash);

                if (pageExist.Count == 0)
                {
                    Page page = new Page()
                    {
                        Id = hash,
                        Hash = hash,
                        Content = pageStream.ToString()
                    };

                    await CosmosDBAccessor<Page>.UpsertAsync(page);
                }

                pageUrl = "https://" + Config.Get("cosmos_domain") +
                    "/dbs/" + CosmosDBAccessor<Page>.GetDatabaseId() +
                    "/colls/" + CosmosDBAccessor<Page>.GetCollectionId() +
                    "/docs/" + hash;

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
                Id = HashUtility.GetSha1HashString($"{doc.Docset}|{doc.Branch}|{doc.Locale}")
            };

            await CosmosDBAccessor<Active>.UpsertAsync(active);
        }
    }
}
