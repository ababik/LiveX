using System;
using System.IO;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LiveX
{
    public class Watcher
    {
        public static Watcher Create(IServiceProvider provider, string path, TimeSpan interval)
        {
            var logger = provider.GetService<ILogger<Watcher>>();
            return new Watcher(logger, path, interval);
        }

        private ILogger Logger { get; }
        private Timer Timer { get; }
        private FileSystemWatcher FileSystemWatcher { get; }
        private bool Set { get; set; }

        public event Action Update;

        public Watcher(ILogger logger, string path, TimeSpan interval)
        {
            Logger = logger;

            FileSystemWatcher = new FileSystemWatcher(path);
            FileSystemWatcher.Changed += HandleFileSystemChange;
            FileSystemWatcher.Created += HandleFileSystemChange;
            FileSystemWatcher.Deleted += HandleFileSystemChange;
            FileSystemWatcher.Renamed += HandleFileSystemChange;
            FileSystemWatcher.IncludeSubdirectories = true;

            Timer = new Timer(interval.TotalMilliseconds);
            Timer.AutoReset = false;
            Timer.Elapsed += HandleTimerElapse;
        }

        public void Start()
        {
            FileSystemWatcher.EnableRaisingEvents = true;
            Timer.Start();
        }

        public void Stop()
        {
            FileSystemWatcher.EnableRaisingEvents = false;
            Timer.Stop();
        }

        private void HandleTimerElapse(object sender, ElapsedEventArgs e)
        {
            if (Set)
            {
                Update.Invoke();
                Set = false;
            }

            Timer.Start();
        }

        private void HandleFileSystemChange(object sender, FileSystemEventArgs e)
        {
            Set = true;
        }
    }
}