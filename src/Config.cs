using System.IO;
using Microsoft.Extensions.Configuration;

namespace docs.host
{
    public static class Config
    {
        private static readonly IConfigurationRoot _iConfig;

        static Config()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true);

            builder = builder.AddEnvironmentVariables();
            builder.AddUserSecrets<Program>();
            _iConfig = builder.Build();
        }

        public static string Get(string name) => _iConfig[name];
    }
}
