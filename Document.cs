using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace docs.host
{
    public class Document
    {
        public string url
        { get; set; }
        public string blob
        { get; set; }
        public string locale
        { get; set; }
        public string version
        { get; set; }
        public string commit
        { get; set; }
    }
}
