using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace docs.host
{
    public static class Method
    {
        public static async Task FindDoc(HttpContext http)
        {
            try
            {
                var filter = await http.ReadAs<Dictionary<string, string>>();

                if (!filter.TryGetValue("branch", out string branch))
                {
                    branch = "master";
                }

                var baseinfos = await DocumentDBRepo<BaseInfo>.GetItemsAsync(b => b.basename == filter["basename"] && b.branch == branch);
                List<string> commits = new List<string>();
                foreach (BaseInfo b in baseinfos)
                {
                    commits.Add(b.commit);
                }

                var documents = await DocumentDBRepo<Document>.GetItemsAsync(
                    d => d.Url == filter["url"] && 
                    d.Locale == filter["locale"] &&
                    d.Version == filter["version"] && 
                    commits.Contains(d.Commit)
                    );

                await http.Write(documents);
            }
            catch
            {
                throw;
            }
        }


        public static async Task PutDoc(HttpContext http)
        {
            var docs = await http.ReadAs<List<Document>>();
            using (var semaphoreSlim = new SemaphoreSlim(50))
            {
                var tasks = docs.Select(async doc =>
                {
                    await semaphoreSlim.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        await DocumentDBRepo<Document>.UpsertItemsAsync(doc);
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }
                });

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            //await Task.WhenAll(docs.Select(doc => DocumentDBRepo<Document>.UpsertItemsAsync(doc)));
        }

        public static async Task HasBlob(HttpContext http)
        {
            var hashes = await http.ReadAs<string[]>();
            var exists = await Task.WhenAll(hashes.Select(hash => BlobStorage.cloudBlobContainer.GetBlobReference(hash).ExistsAsync()));
            await http.Write(exists);
        }

        public static async Task PutBlob(HttpContext http)
        {
            var docs = await http.ReadAs<List<string>>();
            using (var semaphoreSlim = new SemaphoreSlim(50))
            {
                var tasks = docs.Select(async doc =>
                {
                    await semaphoreSlim.WaitAsync().ConfigureAwait(false);
                    try
                    {
                        var hash = Hash(doc);
                        await BlobStorage.cloudBlobContainer.GetBlockBlobReference(hash).UploadTextAsync(doc);
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }
                });

                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
        }


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

        public static string IdGenerator(string locale, string url, string version, string commit)
        {
            //return locale + url.Replace("/", "-") + "-" + version + "-" + commit;
            return Hash(locale + url.Replace("/", "-") + "-" + version + "-" + commit);
        }
    }
}
