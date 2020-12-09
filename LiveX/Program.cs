using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LiveX
{
    public class Program
    {
        public static Options Options { get; private set; }

        public static void Main(string[] args)
        {
            Options = Options.Parse(args);

            if (Options is null)
            {
                return;
            }

            var host = new WebHostBuilder()
                .ConfigureLogging(ConfigureLogger)
                .UseKestrel((x) => ConfigureKestrel(x, Options.Http, Options.Https))
                .UseContentRoot(Options.Path)
                .UseWebRoot(Options.Path)
                .UseStartup<Startup>()
                .Build();

            host.Start();

            var logger = host.Services.GetService<ILogger<Program>>();
            var environment = host.Services.GetService<IWebHostEnvironment>();
            var serverAddressesFeature = host.ServerFeatures.Get<IServerAddressesFeature>();

            PrintRuntimeInfo(environment, serverAddressesFeature);
            OpenBrowser(serverAddressesFeature, logger);

            host.WaitForShutdown();
        }

        public static string ToUrl(string path)
        {
            return Path
                .GetRelativePath(Program.Options.Path, path)
                .Replace('\\', '/');
        }

        private static void ConfigureLogger(ILoggingBuilder builder)
        {
            builder.AddConsole();
            builder.AddFilter("Microsoft", LogLevel.Warning);
        }

        private static void ConfigureKestrel(KestrelServerOptions options, int http, int https)
        {
            options.Listen(IPAddress.Loopback, http);
            options.Listen(IPAddress.Loopback, https, x => x.UseHttps());
        }

        private static void PrintRuntimeInfo(IWebHostEnvironment environment, IServerAddressesFeature serverAddressesFeature)
        {
            var addresses = serverAddressesFeature.Addresses;

            Console.WriteLine($"Content: {environment.WebRootPath}");

            foreach (var address in addresses)
            {
                Console.WriteLine($"Listening: {address}");
            }
        }

        private static void OpenBrowser(IServerAddressesFeature serverAddressesFeature, ILogger logger)
        {
            var address = serverAddressesFeature.Addresses.First();
            OpenBrowser(address, logger);
        }

        private static void OpenBrowser(string url, ILogger logger)
        {
            var action = null as Action;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                action = () => Process.Start("cmd", $"/c start {url}");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                action = () => Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                action = () => Process.Start("open", url);
            }
            else
            {
                action = () => Process.Start(url);
            }

            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Unable to open default web browser.");
            }
        }
    }
}
