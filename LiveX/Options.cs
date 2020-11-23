using System.CommandLine;
using System.CommandLine.Invocation;

namespace LiveX
{
    public class Options
    {
        public string Path { get; }
        public int Http { get; }
        public int Https { get; }

        public Options(string path, int http, int https) =>
            (Path, Http, Https) = (path, http, https);

        public static Options Parse(string[] args)
        {
            var result = null as Options;

            var command = new RootCommand("Light development web server to serve static content with live reloading support");
            command.AddOption(new Option<string>("--path", "Content directory path (skip to use current directory)"));
            command.AddOption(new Option<int>("--http", "HTTP port (skip to use random port)"));
            command.AddOption(new Option<int>("--https", "HTTPS port (skip to use random port)"));

            command.Handler = CommandHandler.Create<string, int, int>((path, http, https) => 
            {
                result = new Options(path ?? ".", http, https);
            });

            command.Invoke(args);

            return result;
        }
    }
}