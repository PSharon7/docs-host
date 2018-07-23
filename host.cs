using System.Configuration;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace docs.host
{
    static class Host
    {
        private readonly static string CollectionDocument = ConfigurationManager.AppSettings["collectionDoc"];
        private readonly static string CollectionBase = ConfigurationManager.AppSettings["collectionBase"];


        static void Main(string[] args)
        {
            DocumentDBRepo<BaseInfo>.Initialize(CollectionBase);
            DocumentDBRepo<Document>.Initialize(CollectionDocument);
            BlobStorage.Initialize();

            WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(services => services.AddRouting())
                .Configure(Configure)
                .Build()
                .Run();
        }

        static void Configure(IApplicationBuilder app) => app.UseRouter(BuildRoutes(app));

        static IRouter BuildRoutes(IApplicationBuilder app) => new RouteBuilder(app)
            .MapPost("finddoc", Method.FindDoc)
            .MapPut("doc", Method.PutDoc)
            .MapPost("hasblob", Method.HasBlob)
            .MapPut("blob", Method.PutBlob)
            .Build();
        
    }
}
