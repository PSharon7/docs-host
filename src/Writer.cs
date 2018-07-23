using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace docs.host
{
    public static class Writer
    {
        public static Task<(string pageUrl, string pageHash)> UploadPage(Stream pageStream, string pagePath)
        {
            throw new NotImplementedException();
        }

        public static Task UploadDocuments(List<Document> documents)
        {
            throw new NotImplementedException();
        }
    }
}
