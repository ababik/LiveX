using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LiveX
{
    public class ScriptInjector
    {
        public static ScriptInjector Create(IServiceProvider provider)
        {
            var logger = provider.GetService<ILogger<ScriptInjector>>();
            return new ScriptInjector(logger);
        }

        private ILogger Logger { get; }
        private FileExtensionContentTypeProvider FileExtensionContentTypeProvider { get; }

        public ScriptInjector(ILogger logger)
        {
            Logger = logger;
            FileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();
        }

        public void Use(IApplicationBuilder app)
        {
            app.UseWhen(IsHtml, x => x.Use(Handle));
        }

        private bool IsHtml(HttpContext context)
        {
            return
                context.Request.Method == HttpMethods.Get &&
                FileExtensionContentTypeProvider.TryGetContentType(context.Request.Path, out var contentType) &&
                contentType == "text/html";
        }

        private async Task Handle(HttpContext context, Func<Task> next)
        {
            Logger.LogInformation("Process html content.");

            var response = context.Response;
            var prevStream = response.Body;
            using var nextStream = new MemoryStream();

            response.Body = nextStream;
            await next();
            response.Body = prevStream;

            if (IsBodyAvailable(response.StatusCode))
            {
                var bytes = nextStream.ToArray();
                var content = Encoding.Default.GetString(bytes);

                if (response.StatusCode == StatusCodes.Status200OK)
                {
                    content = Insert(content);
                    response.ContentLength = Encoding.Default.GetByteCount(content);
                }

                await response.WriteAsync(content);
            }
        }

        private string Insert(string content)
        {
            var index = content.IndexOf("</body>", StringComparison.OrdinalIgnoreCase);

            if (index != -1)
            {
                Logger.LogInformation("Inject script.");

                content = content.Insert(index, "<script src=\"@.js\"></script>");
            }

            return content;
        }

        private static bool IsBodyAvailable(int status)
        {
            return status != StatusCodes.Status204NoContent &&
                   status != StatusCodes.Status205ResetContent &&
                   status != StatusCodes.Status304NotModified;
        }
    }
}