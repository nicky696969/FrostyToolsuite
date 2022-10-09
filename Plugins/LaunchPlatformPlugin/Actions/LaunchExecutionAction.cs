using Frosty.Core;
using Frosty.ModSupport;
using FrostySdk;
using FrostySdk.Interfaces;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

namespace DatapathFixPlugin.Actions
{

    public static class EADesktop
    {
        public static string ProcessName => "EADesktop";

        public static string FullPath {
            get {
                using (RegistryKey lmKey = Registry.LocalMachine.OpenSubKey($"SOFTWARE\\WOW6432Node\\Electronic Arts\\EA Desktop")) {
                    return lmKey.GetValue("DesktopAppPath")?.ToString();
                }
            }
        }
    }

    public class LaunchExecutionAction : ExecutionAction
    {
        public string DatapathFix = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "DatapathFix.exe");

        public override Action<ILogger, PluginManagerType, CancellationToken> PreLaunchAction => new Action<ILogger, PluginManagerType, CancellationToken>((ILogger logger, PluginManagerType type, CancellationToken cancelToken) =>
        {
            string game = Path.Combine(App.FileSystem.BasePath, $"{ProfilesLibrary.ProfileName}.exe");
            ResetGameDirectory(game);
            Thread.Sleep(1000);

            if (Config.Get("DatapathFixEnabled", true) && File.Exists(DatapathFix) && File.Exists(EADesktop.FullPath)) {
                foreach (Process p in Process.GetProcesses()) {
                    if (p.ProcessName == EADesktop.ProcessName) {
                        p.Kill();
                    }
                }

                string cmdArgs = $"-dataPath \"{Path.Combine(App.FileSystem.BasePath, $"ModData\\{App.SelectedPack}")}\"";         

                File.WriteAllText(Path.Combine(App.FileSystem.BasePath, "tmp"), cmdArgs);
                File.Move(game, game.Replace(".exe", ".orig.exe"));
                File.Copy(DatapathFix, game, true);

                FrostyModExecutor.ExecuteProcess(EADesktop.FullPath, "", false, false);

                logger.Log("Waiting For EA Desktop");
                Thread.Sleep(12000);
                WaitForProcess(EADesktop.ProcessName);
            }
        });

        public override Action<ILogger, PluginManagerType, CancellationToken> PostLaunchAction => new Action<ILogger, PluginManagerType, CancellationToken>((ILogger logger, PluginManagerType type, CancellationToken cancelToken) =>
        {
            if (Config.Get("DatapathFixEnabled", true) && File.Exists(DatapathFix) && File.Exists(EADesktop.FullPath)) {
                string game = Path.Combine(App.FileSystem.BasePath, $"{ProfilesLibrary.ProfileName}.exe");

                logger.Log("Waiting For Game");
                Thread.Sleep(8000);
                WaitForProcess(game);

                ResetGameDirectory(game);
            }
        });

        private void ResetGameDirectory(string game) {
            File.Delete(Path.Combine(App.FileSystem.BasePath, "tmp"));
            if (File.Exists(game.Replace(".exe", ".orig.exe"))) {
                File.Delete(game);
                File.Move(game.Replace(".exe", ".orig.exe"), game);
            }
        }

        private void WaitForProcess(string name)
        {
            Stopwatch s = new Stopwatch();
            s.Start();

            // Check only for the next 24 seconds
            while (s.Elapsed < TimeSpan.FromSeconds(24)) {
                Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(name));

                if (processes.Length > 0) {
                    return;
                }
            }
        }
    }
}
