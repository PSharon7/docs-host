using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using docs.host;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

static class Host
{
    public class Document
    {
        public string url;
        public string blob;
        public string locale;
        public string version;
        public string commit;
    }

    private const string EndpointUrl = "https://docs-host.documents.azure.com:443/";
    private const string PrimaryKey = "VmEAHupcmH3hc0VccBmR2jsh7viNtHBh6UX4OkoDtQ3sMX9HRvgD8tFXR1yObtyFHfFmh6c4fzBvuVXSh7Wrzw==";
    private const string defaultBranch = "master";

    private readonly static string DatabaseName = "docs-test";
    private readonly static string CollectionDocument = "Documents";
    private readonly static string CollectionBase = "BaseInfo";

    static readonly DocumentClient s_db = new DocumentClient(new Uri(EndpointUrl), PrimaryKey);
    //static readonly CloudBlobContainer s_blob = CloudStorageAccount.Parse("").CreateCloudBlobClient().GetRootContainerReference();

    static readonly Task<string> s_spFindDocs = CreateStoredProcedure("finddocs", "SELECT * FROM docs");

    static void Main(string[] args)
    {
        WebHost.CreateDefaultBuilder(args)
            .ConfigureServices(services => services.AddRouting())
            .Configure(Configure)
            .Build()
            .Run();
    }

    static void Configure(IApplicationBuilder app) => app.UseRouter(BuildRoutes(app));

    static IRouter BuildRoutes(IApplicationBuilder app) => new RouteBuilder(app)
        .MapGet("finddoc", FindDoc)
        //.MapPut("doc", PutDoc)
        //.MapPost("hasblob", HasBlob)
        //.MapPut("blob", PutBlob)
        .Build();

    static async Task FindDoc(HttpContext http)
    {
        var filter = await http.ReadAs<Dictionary<string, string>>();
        List<string> commits = QueryCommitByBasename("/azure", "");

        var spFindDocs = await s_spFindDocs;
        var result = await s_db.ExecuteStoredProcedureAsync(spFindDocs, (dynamic)null);
        await http.Write((Document[])result.Response);
    }

    /*
    static async Task PutDoc(HttpContext http)
    {
        var docs = await http.ReadAs<Document[]>();
        await Task.WhenAll(docs.Select(doc => s_db.UpsertDocumentAsync("", doc, disableAutomaticIdGeneration: true)));
    }

    static async Task PutBlob(HttpContext http)
    {
        var stream = new MemoryStream();
        await http.Request.Body.CopyToAsync(stream);
        stream.Position = 0;
        var hash = Hash(stream);
        stream.Position = 0;
        await s_blob.GetBlockBlobReference(hash).UploadFromStreamAsync(stream);
        await http.Response.WriteAsync(hash);
    }

    static async Task HasBlob(HttpContext http)
    {
        var hashes = await http.ReadAs<string[]>();
        var exists = await Task.WhenAll(hashes.Select(hash => s_blob.GetBlobReference(hash).ExistsAsync()));
        await http.Write(exists);
    }
    */

    static async Task<T> ReadAs<T>(this HttpContext http) => JsonConvert.DeserializeObject<T>(await new StreamReader(http.Request.Body).ReadToEndAsync());

    static Task Write<T>(this HttpContext http, T value) => http.Response.WriteAsync(JsonConvert.SerializeObject(value));

    static string Hash(Stream stream)
    {
        using (var sha1 = SHA1.Create())
        {
            return WebEncoders.Base64UrlEncode(sha1.ComputeHash(stream));
        }
    }

    static string Hash(string str)
    {
        using (var sha1 = SHA1.Create())
        {
            return WebEncoders.Base64UrlEncode(sha1.ComputeHash(Encoding.UTF8.GetBytes(str)));
        }
    }

    static async Task<string> CreateStoredProcedure(string name, string code)
    {
        var link = $"{name}-{Hash(code)}";
        await s_db.CreateStoredProcedureAsync(link, new StoredProcedure { Body = code });
        return link;
    }

    static List<string> QueryCommitByBasename(string basename, string branch)
    {
        if (string.IsNullOrWhiteSpace(branch))
        {
            branch = defaultBranch;
        }

        IQueryable<BaseInfo> query = s_db.CreateDocumentQuery<BaseInfo>(
            UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionBase), 
            $"SELECT * FROM BaseInfo b WHERE b.basement = '{basename}'",
            new FeedOptions { });

        List<string> commits = new List<string>();
        foreach (BaseInfo b in query)
        {
            commits.Add(b.commit);
        }
        
        return commits;
    }

    static IQueryable<Document> QureyDocument(string locale, string assetID, string version, string blob, List<string> commit)
    {
        var query = s_db.CreateDocumentQuery(
            UriFactory.CreateDocumentCollectionUri(DatabaseName, CollectionDocument),
            $"SELECT * " +
            $"FROM Documents d " +
            $"WHERE d.locale == '{locale}' AND d.version = '{version}' AND d.blob = '{blob}' AND d.assetID = '{assetID}' AND d.commit in '{commit}'",
            new FeedOptions()
            );
        return (IQueryable<Document>)query;
    }
}
