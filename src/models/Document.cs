namespace docs.host
{
    public class Document
    {
        public string Url { get; set; }
        public string Locale { get; set; }
        public string Branch { get; set; }
        public string Version { get; set; }
        public string Docset { get; set; }
        public string ActiveEtag { get; set; }
        public string PageHash { get; set; }
        public string PageUrl { get; set; }
        public string PageType { get; set; }
        public string Title { get; set; }
        public string Layout { get; set; }
        public bool IsDynamicRendering { get; set; }
        public string ContentType { get; set; }
    }
}
