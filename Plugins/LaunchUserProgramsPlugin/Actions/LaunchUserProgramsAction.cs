using Frosty.Core;
using Frosty.ModSupport;
using FrostySdk;
using FrostySdk.Interfaces;
using LaunchUserProgramsPlugin.Options;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace LaunchUserProgramsPlugin.Actions
{
    public class LaunchUserProgramsAction : ExecutionAction
    {
        public override Action<ILogger, PluginManagerType, CancellationToken> PreLaunchAction => new Action<ILogger, PluginManagerType, CancellationToken>((ILogger logger, PluginManagerType type, CancellationToken cancelToken) =>
        {
            string frostyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            DirectoryInfo di = new DirectoryInfo($"{frostyDir}\\Plugins\\UserPrograms\\");
            if (!di.Exists) di.Create();

            if (Config.Get("UserProgramLaunchingEnabled", false, ConfigScope.Game) && di.GetFiles("*.exe") != null) {
                foreach (var file in di.GetFiles("*.exe")) {
                    if (file.Name.Contains("{{")) {
                        string fileName = file.Name.Substring(file.Name.IndexOf("}}") + 2);
                        string pack = file.Name.Substring(2, file.Name.IndexOf("}}") - 2);
                        if (App.SelectedPack == pack) {
                            FrostyModExecutor.ExecuteProcess(file.FullName, "");
                            logger.Log($"Waiting for {fileName}");
                            try {
                                WaitForProcess(Path.GetFileNameWithoutExtension(file.Name), cancelToken);
                            }
                            catch (OperationCanceledException) {
                            }
                        } 
                    }
                    else {
                        FrostyModExecutor.ExecuteProcess(file.FullName, "");
                        logger.Log($"Waiting for {file.Name}");
                        try {
                            WaitForProcess(Path.GetFileNameWithoutExtension(file.Name), cancelToken);
                        }
                        catch (OperationCanceledException) {
                        }
                    }
                }
            }
        });

        public override Action<ILogger, PluginManagerType, CancellationToken> PostLaunchAction => new Action<ILogger, PluginManagerType, CancellationToken>((ILogger logger, PluginManagerType type, CancellationToken cancelToken) =>
        {
            string frostyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            DirectoryInfo di = new DirectoryInfo($"{frostyDir}\\Plugins\\UserPrograms\\");
            if (!di.Exists) di.Create();

            if (Config.Get("UserProgramLaunchingEnabled", false, ConfigScope.Game) && di.GetFiles("*.exe") != null) {
                logger.Log("Waiting for game");

                // find game process to check when it closes
                try {
                    Process gameProcess = WaitForProcess(ProfilesLibrary.ProfileName, cancelToken);
                    if (gameProcess != null) {
                        gameProcess.EnableRaisingEvents = true;
                        gameProcess.Exited += OnGameProcessExited;
                    }
                }
                catch (OperationCanceledException) {
                }
            }
        });

        private void OnGameProcessExited(object sender, EventArgs e)
        {
            string frostyDir = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            DirectoryInfo di = new DirectoryInfo($"{frostyDir}\\Plugins\\UserPrograms");
            if (!di.Exists) di.Create();
            foreach (var file in di.GetFiles("*.exe")) {
                KillProcess(Path.GetFileNameWithoutExtension(file.Name));
            }
            
        }

        private Process GetProcessByName(string name)
        {
            Process[] processes = Process.GetProcessesByName(name);
            if (processes.Length > 0)
                return processes[0];
            else
                return null;
        }

        private Process WaitForProcess(string name, CancellationToken cancelToken)
        {
            Process gameProcess = null;
            while (true)
            {
                cancelToken.ThrowIfCancellationRequested();

                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        cancelToken.ThrowIfCancellationRequested();

                        string processFilename = process.MainModule?.ModuleName;
                        if (string.IsNullOrEmpty(processFilename))
                            continue;

                        FileInfo fi = new FileInfo(processFilename);
                        if (fi.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            gameProcess = process;
                            break;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                if (gameProcess != null)
                {
                    while (gameProcess.MainWindowHandle == IntPtr.Zero)
                        Thread.Sleep(200);

                    return gameProcess;
                }
            }
        }

        private void KillProcess(string process)
        {
            Process Processes = GetProcessByName(process);
            if (Processes != null)
                Processes.Kill();
        }
    }
}
