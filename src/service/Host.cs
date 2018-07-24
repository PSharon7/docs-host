using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace docs.host
{
    public static class Host
    {
        public static void Create()
        {
            WebHost.CreateDefaultBuilder()
                .UseStartup<Startup>()
                .Build()
                .Run();
        }
    }
}
