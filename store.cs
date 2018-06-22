using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

static class Store
{
    class Document
    {
        public string url;
        public string blob;
        public string locale;
        public string moniker;
        public string commit;
        public string docset;
    }

    static readonly DocumentClient s_db = new DocumentClient(new Uri(""), "");
    static readonly CloudBlobContainer s_blob = CloudStorageAccount.Parse("").CreateCloudBlobClient().GetRootContainerReference();

    static void Main(string[] args) => WebHost.CreateDefaultBuilder(args).Configure(Configure).Build().Run();

    static void Configure(IApplicationBuilder app) => app.UseRouter(BuildRoutes(app));

    static IRouter BuildRoutes(IApplicationBuilder app) => new RouteBuilder(app)
        .MapPost("finddoc", FindDoc)
        .MapPut("doc", PutDoc)
        .MapPost("hasblob", HasBlob)
        .MapPut("blob", PutBlob)
        .Build();

    static async Task FindDoc(HttpContext http)
    {
        var filter = await http.ReadAs<Dictionary<string, string>>();
        var result = await s_db.ExecuteStoredProcedureAsync("", (dynamic)null);
        await http.Write((Document[])result.Response);
    }

    static async Task PutDoc(HttpContext http)
    {
        var docs = await http.ReadAs<Document[]>();
        await Task.WhenAll(docs.Select(doc => s_db.UpsertDocumentAsync("", doc, disableAutomaticIdGeneration: true)));
    }

    static async Task PutBlob(HttpContext http)
    {
        var bytes = new MemoryStream();
        await http.Request.Body.CopyToAsync(bytes);
        using (var sha1 = SHA1.Create())
        {
            bytes.Position = 0;
            var hash = WebEncoders.Base64UrlEncode(sha1.ComputeHash(bytes));
            bytes.Position = 0;
            await s_blob.GetBlockBlobReference(hash).UploadFromStreamAsync(bytes);
            await http.Response.WriteAsync(hash);
        }
    }

    static async Task HasBlob(HttpContext http)
    {
        var hashes = await http.ReadAs<string[]>();
        var exists = await Task.WhenAll(hashes.Select(hash => s_blob.GetBlobReference(hash).ExistsAsync()));
        await http.Write(exists);
    }

    static async Task<T> ReadAs<T>(this HttpContext http) => JsonConvert.DeserializeObject<T>(await new StreamReader(http.Request.Body).ReadToEndAsync());

    static Task Write<T>(this HttpContext http, T value) => http.Response.WriteAsync(JsonConvert.SerializeObject(value));
}
