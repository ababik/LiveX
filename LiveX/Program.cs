using System;
using System.Diagnostics;
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

            var environment = host.Services.GetService<IWebHostEnvironment>();
            var serverAddressesFeature = host.ServerFeatures.Get<IServerAddressesFeature>();

            PrintRuntimeInfo(environment, serverAddressesFeature);
            OpenBrowser(serverAddressesFeature);

            host.WaitForShutdown();
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

        private static void OpenBrowser(IServerAddressesFeature serverAddressesFeature)
        {
            var address = serverAddressesFeature.Addresses.First();
            OpenBrowser(address);
        }

        public static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
