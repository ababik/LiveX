using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private bool IsGeneralUpdate { get; set; }
        private bool IsStyleUpdate { get; set; }
        private HashSet<string> StylesToUpdate { get; }

        public event Action OnGeneralUpdate;
        public event Action<string> OnStyleUpdate;

        public Watcher(ILogger logger, string path, TimeSpan interval)
        {
            Logger = logger;

            StylesToUpdate = new HashSet<string>();

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
            if (IsGeneralUpdate)
            {
                OnGeneralUpdate.Invoke();

                IsGeneralUpdate = false;
                IsStyleUpdate = false;
            }
            
            if (IsStyleUpdate)
            {
                StylesToUpdate
                    .ToList()
                    .ForEach(OnStyleUpdate.Invoke);

                IsStyleUpdate = false;
                StylesToUpdate.Clear();
            }

            Timer.Start();
        }

        private void HandleFileSystemChange(object sender, FileSystemEventArgs e)
        {
            if (CheckSkipDirectories(e.FullPath))
            {
                return;
            }

            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                var extension = Path.GetExtension(e.Name);

                if (CheckStyleExtension(extension))
                {
                    IsStyleUpdate = true;
                    StylesToUpdate.Add(e.FullPath);
                    return;
                }
            }

            IsGeneralUpdate = true;
        }

        private static bool CheckSkipDirectories(string path)
        {
            return path.Contains("node_modules");
        }

        private static bool CheckStyleExtension(string extension)
        {
            return string.Equals(extension, ".css", StringComparison.OrdinalIgnoreCase);
        }
    }
}