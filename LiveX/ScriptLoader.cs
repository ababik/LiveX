using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LiveX
{
    public class ScriptLoader
    {
        public static ScriptLoader Create(IServiceProvider provider)
        {
            var logger = provider.GetService<ILogger<ScriptLoader>>();
            return new ScriptLoader(logger);
        }

        private ILogger Logger { get; }

        public ScriptLoader(ILogger logger)
        {
            Logger = logger;
        }

        public void Use(IApplicationBuilder app)
        {
            app.Use(Handle);
        }

        private static async Task Handle(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path == "/@.js")
            {
                var directory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var content = await File.ReadAllTextAsync(Path.Combine(directory, "livex.js"));

                var protocol = context.Request.IsHttps ? "wss" : "ws";
                var host = context.Request.Host.Value;
                content += $"(\"{protocol}://{host}/ws\")";

                var bytes = Encoding.Default.GetBytes(content);

                context.Response.ContentLength = bytes.Length;
                await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);

                return;
            }

            await next();
        }
    }
}