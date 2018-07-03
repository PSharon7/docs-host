using System;
using System.Collections.Generic;
using System.Configuration;
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

    private readonly static string CollectionDocument = ConfigurationManager.AppSettings["collectionDoc"];
    private readonly static string CollectionBase = ConfigurationManager.AppSettings["collectionBase"];
    

    //static readonly CloudBlobContainer s_blob = CloudStorageAccount.Parse("").CreateCloudBlobClient().GetRootContainerReference();
    

    static void Main(string[] args)
    {
        DocumentDBRepo<BaseInfo>.Initialize(CollectionBase);
        DocumentDBRepo<docs.host.Document>.Initialize(CollectionDocument);

        WebHost.CreateDefaultBuilder(args)
            .ConfigureServices(services => services.AddRouting())
            .Configure(Configure)
            .Build()
            .Run();
    }

    static void Configure(IApplicationBuilder app) => app.UseRouter(BuildRoutes(app));

    static IRouter BuildRoutes(IApplicationBuilder app) => new RouteBuilder(app)
        .MapPost("finddoc", FindDoc)
        .MapPut("doc", PutDoc)
        //.MapPost("hasblob", HasBlob)
        //.MapPut("blob", PutBlob)
        .Build();

    static async Task FindDoc(HttpContext http)
    {
        var filter = await http.ReadAs<Dictionary<string, string>>();
        var baseinfos = await DocumentDBRepo<BaseInfo>.GetItemsAsync(b => b.basename == "/azure" && b.branch == "master");
        List<string> commits = new List<string>();
        foreach (BaseInfo b in baseinfos)
        {
            commits.Add(b.commit);
        }

        var documents = await DocumentDBRepo<docs.host.Document>.GetItemsAsync(d => commits.Contains(d.commit));

        await http.Write(documents);
    }
    

    static async Task PutDoc(HttpContext http)
    {
        var docs = await http.ReadAs<docs.host.Document[]>();
        docs.host.Document d1 = new docs.host.Document();
        d1.url = "/azure/new1";
        d1.blob = "Empty";
        d1.locale = "zh-cn";
        d1.version = "1";
        d1.commit = "123";

        docs.host.Document d2 = new docs.host.Document();
        d2.url = "/azure/new2";
        d2.blob = "Empty";
        d2.locale = "zh-cn";
        d2.version = "2";
        d2.commit = "124";

        var temp = new docs.host.Document[] { d1, d2 };

        await Task.WhenAll(temp.Select(doc => DocumentDBRepo<docs.host.Document>.UpsertItemsAsync(doc)));
    }

    /*
    static async Task HasBlob(HttpContext http)
    {
        var hashes = await http.ReadAs<string[]>();
        var exists = await Task.WhenAll(hashes.Select(hash => s_blob.GetBlobReference(hash).ExistsAsync()));
        await http.Write(exists);
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

    /*
    static async Task<string> CreateStoredProcedure(string name, string code)
    {
        var link = $"{name}-{Hash(code)}";
        await s_db.CreateStoredProcedureAsync(link, new StoredProcedure { Body = code });
        return link;
    }
    */
}
