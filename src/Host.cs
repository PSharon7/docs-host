using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace docs.host
{
    static class Host
    {
        static void Main(string[] args)
        {
            BlobAccessor.Initialize();
            WebHost.CreateDefaultBuilder(args)
                .Build()
                .Run();
        }
    }
}
