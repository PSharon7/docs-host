using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Threading;
using System.Collections.Concurrent;

namespace docs.host
{
    public static class CosmosDBAccessor<T> where T : class
    {
        private static readonly string s_databaseId = ConfigurationManager.AppSettings["cosmos_database"];
        private static readonly Uri endpointUri = new Uri(ConfigurationManager.AppSettings["cosmos_endpoint"]);
        private static readonly DocumentClient client = new DocumentClient(endpointUri, ConfigurationManager.AppSettings["cosmos_authKey"]);
        private static readonly ConcurrentDictionary<Type, Task<Uri>> documentCollectionUris = new ConcurrentDictionary<Type, Task<Uri>>();

        public static async Task<IEnumerable<T>> QueryAsync(Expression<Func<T, bool>> predicate)
        {
            var collectionUri = await GetCollection();
            IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
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
                    await client.UpsertDocumentAsync(collectionUri, item);
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

        private static Task<Uri> GetCollection() => documentCollectionUris.GetOrAdd(typeof(T), async key =>
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
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(databaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDatabaseAsync(new Database { Id = databaseId });
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
                await client.ReadDocumentCollectionAsync(UriFactory.CreateDocumentCollectionUri(s_databaseId, collectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentCollectionAsync(
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
