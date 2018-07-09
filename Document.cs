using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace docs.host
{
    [Serializable]
    public class Document
    {
        public string Url
        { get; set; }
        public string Blob
        { get; set; }
        public string Locale
        { get; set; }
        public string Version
        { get; set; }
        public string Commit
        { get; set; }
        public string Id
        { get; set; }
    }
}
