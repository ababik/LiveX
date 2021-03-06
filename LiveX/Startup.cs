using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace LiveX
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<CacheValidator>(CacheValidator.Create);
            services.AddSingleton<ScriptInjector>(ScriptInjector.Create);
            services.AddSingleton<ScriptLoader>(ScriptLoader.Create);
            services.AddSingleton<Communicator>(Communicator.Create);
            services.AddSingleton<Watcher>((x) => Watcher.Create(x, Program.Options.Path, TimeSpan.FromSeconds(1)));

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyHeader();
                    builder.AllowAnyMethod();
                    builder.AllowAnyOrigin();
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var cacheValidator = app.ApplicationServices.GetService<CacheValidator>();
            var scriptInjector = app.ApplicationServices.GetService<ScriptInjector>();
            var scriptLoader = app.ApplicationServices.GetService<ScriptLoader>();
            var communicator = app.ApplicationServices.GetService<Communicator>();
            var watcher = app.ApplicationServices.GetService<Watcher>();

            app.UseCors();
            app.UseWebSockets();

            cacheValidator.Use(app);
            scriptInjector.Use(app);
            scriptLoader.Use(app);
            communicator.Use(app);

            app.UseStaticFiles();
            app.UseDirectoryBrowser();

            watcher.OnGeneralUpdate += communicator.NotifyGeneralUpdate;
            watcher.OnStyleUpdate += communicator.NotifyStyleUpdate;
            watcher.Start();
        }
    }
}
