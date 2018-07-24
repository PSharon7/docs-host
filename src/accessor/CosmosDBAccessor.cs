using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace docs.host
{
    public static class CosmosDBAccessor<T> where T : class
    {
        private static readonly string s_databaseId = Config.Get("cosmos_database");
        private static readonly Uri s_endpointUri = new Uri(Config.Get("cosmos_endpoint"));
        private static readonly DocumentClient s_client = new DocumentClient(s_endpointUri, Config.Get("cosmos_authkey"));
        private static readonly ConcurrentDictionary<Type, Task<Uri>> s_documentCollectionUris = new ConcurrentDictionary<Type, Task<Uri>>();
        
        public static async Task<IEnumerable<T>> QueryAsync(Expression<Func<T, bool>> predicate)
        {
            var collectionUri = await GetCollection();
            IDocumentQuery<T> query = s_client.CreateDocumentQuery<T>(
                collectionUri)
                .Where(predicate)
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public static async Task UpsertAsync(T item, int retryCount = 10)
        {
            var collectionUri = await GetCollection();
            var queryDone = false;
            var retry = 0;
            while (!queryDone && retry++ < retryCount)
            {
                try
                {
                    await s_client.UpsertDocumentAsync(collectionUri, item);
                    queryDone = true;
                }
                catch (DocumentClientException documentClientException)
                {
                    var statusCode = (int)documentClientException.StatusCode;
                    if (statusCode == 429 || statusCode == 503)
                        Thread.Sleep(documentClientException.RetryAfter);
                    else
                        throw;
                }
                catch (AggregateException aggregateException)
                {
                    if (aggregateException.InnerException.GetType() == typeof(DocumentClientException))
                    {
                        var docExcep = aggregateException.InnerException as DocumentClientException;
                        var statusCode = (int)docExcep.StatusCode;
                        if (statusCode == 429 || statusCode == 503)
                            Thread.Sleep(docExcep.RetryAfter);
                        else
                            throw;
                    }
                }
            }
        }

        public static string GetDatabaseId()
        {
            return s_databaseId;
        }

        public static string GetCollectionId()
        {
            return GetFriendlyName(typeof(T));
        }

        private static Task<Uri> GetCollection() => s_documentCollectionUris.GetOrAdd(typeof(T), async key =>
        {
            var collectionId = GetFriendlyName(typeof(T));
            var collectionUri = UriFactory.CreateDocumentCollectionUri(s_databaseId, collectionId);

            await CreateDatabaseIfNotExistsAsync(s_databaseId);
            await CreateCollectionIfNotExistsAsync(s_databaseId, collectionId);

            return collectionUri;
        });

        private static async Task CreateDatabaseIfNotExistsAsync(string databaseId)
        {
            try
            {
                await s_client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(databaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await s_client.CreateDatabaseAsync(new Database { Id = databaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync(string databaseId, string collectionId)
        {
            try
            {
                await s_client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(s_databaseId, collectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await s_client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(databaseId),
                        new DocumentCollection { Id = collectionId },
                        new RequestOptions { OfferThroughput = 1000 });
                }
                else
                {
                    throw;
                }
            }
        }

        private static string GetFriendlyName(Type type)
        {
            if (type.IsGenericType)
                return type.Name.Split('`')[0] + "Of" + string.Join("And", type.GetGenericArguments().Select(x => GetFriendlyName(x)).ToArray());

            return type.Name;
        }
    }
}
