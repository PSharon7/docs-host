using System;
using System.CommandLine;
using System.Threading.Tasks;

namespace docs.host
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            await BlobAccessor.Initialize();
            var (command, basePath, branch, locale) = ParseCommandLineOptions(args);
            switch (command)
            {
                case "host":
                    Host.Create();
                    break;
                case "migrate":
                    await Publish.Migrate(basePath, branch, locale, 1);
                    break;
            }

            Console.Read();
            return 0;
        }
        private static (string command, string basePath, string branch, string locale) ParseCommandLineOptions(string[] args)
        {
            var command = "host";
            var basePath = "";
            var branch = "";
            var locale = "";

            if (args.Length == 0)
            {
                // Show usage when just running `docs-host`
                args = new[] { "--help" };
            }

            ArgumentSyntax.Parse(args, syntax =>
            {
                // usage: docs-host host
                syntax.DefineCommand("host", ref command, "Host the service for serving the requests");

                // usage: docs-host migrate [base-path] [--branch branch] [--locale locale]
                syntax.DefineCommand("migrate", ref command, "Migrate documents to cosmos database for hosting");
                syntax.DefineOption("locale", ref locale, "Which locale you want to migrate.");
                syntax.DefineOption("branch", ref branch, "Which branch you want to migrate");
                syntax.DefineParameter("base-path", ref basePath, "The base path you want to migrate");
            });

            return (command, basePath, branch, locale);
        }
    }
}
