using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace LiveX
{
    public class CacheValidator
    {
        public static CacheValidator Create(IServiceProvider provider)
        {
            var logger = provider.GetService<ILogger<CacheValidator>>();
            return new CacheValidator(logger);
        }

        private ILogger Logger { get; }

        public CacheValidator(ILogger logger)
        {
            Logger = logger;
        }

        public void Use(IApplicationBuilder app)
        {
            app.Use(Handle);
        }

        private static async Task Handle(HttpContext context, Func<Task> next)
        {
            var headers = context.Response.GetTypedHeaders();
            var cacheControl = new CacheControlHeaderValue() { NoStore = true };
            headers.CacheControl = cacheControl;

            await next();
        }
    }
}