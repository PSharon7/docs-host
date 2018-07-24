using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace docs.host
{
    public static class Writer
    {
        public static Task<(string pageUrl, string pageHash)> UploadPage(Stream pageStream, string contentType)
        {
            throw new NotImplementedException();
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
