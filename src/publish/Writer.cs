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
            if (contentType == "png" || contentType == "jpg" || contentType == "pdf")
            {
                bool blobExist = await BlobAccessor.cloudBlobContainer.GetBlockBlobReference(hash).ExistsAsync();

                if (!blobExist)
                {
                    await BlobAccessor.cloudBlobContainer.GetBlockBlobReference(hash).UploadFromStreamAsync(pageStream);
                }

                pageUrl = ConfigurationManager.AppSettings["schema"] + ConfigurationManager.AppSettings["blob_domain"] + ConfigurationManager.AppSettings["blob_endpoint"] + hash;
            }
            else
            {
                ICollection<Page> pageExist = (ICollection<Page>)CosmosDBAccessor<Page>.QueryAsync(p => p.Hash == hash);
                if (pageExist.Count == 0)
                {
                    Page page = new Page()
                    {
                        Hash = hash,
                        Content = pageStream.ToString()
                    };

                    await CosmosDBAccessor<Page>.UpsertAsync(page);
                }

                pageUrl = ConfigurationManager.AppSettings["schema"] + ConfigurationManager.AppSettings["cosmos_domain"] + ConfigurationManager.AppSettings["cosmos_endpoint"] + hash;

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
