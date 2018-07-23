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

namespace docs.host
{
    public static class DocumentDBRepo<T> where T : class
    {

        private readonly static string EndpointUrl = ConfigurationManager.AppSettings["endpoint"];
        private readonly static string PrimaryKey = ConfigurationManager.AppSettings["authKey"];
        private readonly static string DatabaseId = ConfigurationManager.AppSettings["database"];

        public static DocumentClient client;
        public static Uri endpointUri;
        public static Uri documentCollectionUri;

        private static string CollectionId = "";

        public static void Initialize(string Id)
        {
            CollectionId = Id;
            
            endpointUri = new Uri(EndpointUrl);
            documentCollectionUri = UriFactory.CreateDocumentCollectionUri(DatabaseId, CollectionId);

            client = new DocumentClient(endpointUri, PrimaryKey);

            CreateDatabaseIfNotExistsAsync().Wait();
            CreateCollectionIfNotExistsAsync().Wait();
        }

        private static async Task CreateDatabaseIfNotExistsAsync()
        {
            try
            {
                await client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDatabaseAsync(new Database { Id = DatabaseId });
                }
                else
                {
                    throw;
                }
            }
        }

        private static async Task CreateCollectionIfNotExistsAsync()
        {
            try
            {
                await client.ReadDocumentCollectionAsync(documentCollectionUri);
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentCollectionAsync(
                        UriFactory.CreateDatabaseUri(DatabaseId),
                        new DocumentCollection { Id = CollectionId },
                        new RequestOptions { OfferThroughput = 1000 });
                }
                else
                {
                    throw;
                }
            }
        }

        //get commit by basename & branch
        public static async Task<IEnumerable<T>> GetItemsAsync(Expression<Func<T, bool>> predicate)
        {
            IDocumentQuery<T> query = client.CreateDocumentQuery<T>(
                documentCollectionUri)
                .Where(predicate)
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;
        }

        public static async Task UpsertItemsAsync(T item)
        {
            var queryDone = false;
            while (!queryDone)
            {
                try
                {
                    await client.UpsertDocumentAsync(documentCollectionUri, item);
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
    }
}
