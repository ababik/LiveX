using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LiveX
{
    public class Communicator
    {
        public static Communicator Create(IServiceProvider provider)
        {
            var logger = provider.GetService<ILogger<Communicator>>();
            var lifetime = provider.GetService<IHostApplicationLifetime>();
            return new Communicator(logger, lifetime);
        }

        private static byte[] RefreshMessageBytes { get; } = Encoding.Default.GetBytes("@");

        private ILogger Logger { get; }
        private IHostApplicationLifetime Lifetime { get; }
        private HashSet<WebSocket> WebSockets { get; }

        public Communicator(ILogger logger, IHostApplicationLifetime lifetime)
        {
            Logger = logger;
            Lifetime = lifetime;
            WebSockets = new HashSet<WebSocket>();
        }

        public void Use(IApplicationBuilder app)
        {
            app.Use(Handle);
        }

        private async Task Handle(HttpContext context, Func<Task> next)
        {
            if (context.Request.Path != "/ws")
            {
                await next();
                return;
            }

            if (context.WebSockets.IsWebSocketRequest is false)
            {
                context.Response.StatusCode = 400;
                return;
            }

            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            WebSockets.Add(webSocket);

            Logger.LogInformation("Accept web socket. Count: " + WebSockets.Count);

            var buffer = new byte[byte.MaxValue];

            while (webSocket.State.HasFlag(WebSocketState.Open))
            {
                try
                {
                    var segment = new ArraySegment<byte>(buffer);
                    var result = await webSocket.ReceiveAsync(segment, Lifetime.ApplicationStopping);

                    if (result.CloseStatus.HasValue)
                    {
                        break;
                    }
                }
                catch (Exception ex) when (ex is WebSocketException || ex is OperationCanceledException)
                {
                    break;
                }
            }

            WebSockets.Remove(webSocket);
            
            if (webSocket.State != WebSocketState.Closed && webSocket.State != WebSocketState.Aborted)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, Lifetime.ApplicationStopping);
            }

            Logger.LogInformation("Close web socket. Count: " + WebSockets.Count);
        }

        public void NotifyGeneralUpdate()
        {
            Logger.LogInformation("Notify general update. Count: " + WebSockets.Count);

            Notify(RefreshMessageBytes);
        }

        public void NotifyStyleUpdate(string path)
        {
            Logger.LogInformation($"Notify style update ({path}). Count: " + WebSockets.Count);

            var url = Program.ToUrl(path);
            var data = Encoding.Default.GetBytes("@css:" + url);

            Notify(data);
        }

        private void Notify(byte[] data)
        {
            WebSockets
                .ToList()
                .ForEach((x) => TryNotify(x, data));
        }

        private async Task TryNotify(WebSocket webSocket, byte[] data)
        {
            try
            {
                var segment = new ArraySegment<byte>(data);
                await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, Lifetime.ApplicationStopping);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Fail to notify.");
            }
        }
    }
}