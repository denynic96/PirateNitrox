﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NitroxLauncher.Events;
using NitroxLauncher.Pages;
using NitroxLauncher.Patching;
using NitroxModel;
using NitroxModel.Helper;
using NitroxModel.Logger;

namespace NitroxLauncher
{
  public class LauncherLogic : IDisposable, INotifyPropertyChanged
  {
    private static App _app = Application.Current as App;

    public static string Version => Assembly.GetAssembly(typeof(Extensions)).GetName().Version.ToString();

    public const string RELEASE_PHASE = "ALPHA";
    private NitroxEntryPatch nitroxEntryPatch;
    private string subnauticaPath;
    private Process serverProcess;
    private Process gameProcess;
    private bool isEmbedded;

    public string SubnauticaPath
    {
      get => subnauticaPath;
      private set
      {
        value = Path.GetFullPath(value); // Ensures the path looks alright (no mixed / and \ path separators)
        subnauticaPath = value;
        OnPropertyChanged();
      }
    }

    public bool ServerRunning => !serverProcess?.HasExited ?? false;

    public event EventHandler<ServerStartEventArgs> ServerStarted;
    public event DataReceivedEventHandler ServerDataReceived;
    public event EventHandler ServerExited;

    public void Dispose()
    {
      Application.Current.MainWindow?.Hide();
      if (isEmbedded)
      {
        _app.LauncherLogic.SendServerCommand("stop\n");
      }

      try
      {
        nitroxEntryPatch?.Remove();
      }
      catch (Exception)
      {
        // Ignored
      }

      gameProcess?.Dispose();
      serverProcess?.Dispose();
      serverProcess = null; // Indicate the process is dead now.
    }

    public void WriteToServer(string inputText)
    {
      if (ServerRunning)
      {
        try
        {
          serverProcess.StandardInput.WriteLine(inputText);
        }
        catch (Exception)
        {
          // Ignore errors while writing to process
        }
      }
    }

    public async Task<string> SetTargetedSubnauticaPath(string path)
    {
      if (SubnauticaPath == path || !Directory.Exists(path))
      {
        return null;
      }
      SubnauticaPath = path;

      return await Task.Factory.StartNew(() =>
          {
            PirateDetection.TriggerOnDirectory(path);
            File.WriteAllText("path.txt", path);
            if (nitroxEntryPatch?.IsApplied == true)
            {
              nitroxEntryPatch.Remove();
            }
            nitroxEntryPatch = new NitroxEntryPatch(path);

            if (Path.GetFullPath(path).StartsWith(AppHelper.ProgramFileDirectory, StringComparison.OrdinalIgnoreCase))
            {
              AppHelper.RestartAsAdmin();
            }

            return path;
          },
          CancellationToken.None,
          TaskCreationOptions.None,
          TaskScheduler.FromCurrentSynchronizationContext());
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public bool IsSubnauticaDirectory(string directory)
    {
      if (string.IsNullOrWhiteSpace(directory))
      {
        return false;
      }

      return Directory.EnumerateFileSystemEntries(directory, "*.exe")
          .Any(file => Path.GetFileName(file)?.Equals("subnautica.exe", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    internal async Task StartSingleplayerAsync()
    {
#if RELEASE
      if (Process.GetProcessesByName("Subnautica").Length > 0)
      {
        throw new Exception("An instance of Subnautica is already running");
      }
#endif
      nitroxEntryPatch.Remove();
      gameProcess = StartSubnautica() ?? await WaitForProcessAsync();
    }

    internal async Task StartMultiplayerAsync()
    {
#if RELEASE
      if (Process.GetProcessesByName("Subnautica").Length > 0)
      {
        throw new Exception("An instance of Subnautica is already running");
      }
#endif
      // Store path where launcher is in AppData for Nitrox bootstrapper to read
      string nitroxAppData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nitrox");
      Directory.CreateDirectory(nitroxAppData);
      File.WriteAllText(Path.Combine(nitroxAppData, "launcherpath.txt"), Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

      // TODO: The launcher should override FileRead win32 API for the Subnautica process to give it the modified Assembly-CSharp from memory 
      string bootloaderName = "Nitrox.Bootloader.dll";
      try
      {
        File.Copy(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "lib", bootloaderName), Path.Combine(subnauticaPath, "Subnautica_Data", "Managed", bootloaderName), true);
      }
      catch (IOException ex)
      {
        Log.Error(ex, "Unable to move bootloader dll to Managed folder. Still attempting to launch because it might exist from previous runs.");
      }

      nitroxEntryPatch.Remove(); // Remove any previous instances first.
      nitroxEntryPatch.Apply();

      gameProcess = StartSubnautica() ?? await WaitForProcessAsync();
    }

    internal Process StartServer(bool standalone)
    {
      if (ServerRunning)
      {
        throw new Exception("An instance of Nitrox Server is already running");
      }

      string launcherDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
      string serverPath = Path.Combine(launcherDir, "NitroxServer-Subnautica.exe");
      ProcessStartInfo startInfo = new ProcessStartInfo(serverPath);
      startInfo.WorkingDirectory = launcherDir;

      if (!standalone)
      {
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardInput = true;
        startInfo.CreateNoWindow = true;
      }

      serverProcess = Process.Start(startInfo);
      if (serverProcess != null)
      {
        serverProcess.EnableRaisingEvents = true; // Required for 'Exited' event from process.

        if (!standalone)
        {
          serverProcess.OutputDataReceived += ServerProcessOnOutputDataReceived;
          serverProcess.BeginOutputReadLine();
        }
        serverProcess.Exited += (sender, args) => OnEndServer();
        OnStartServer(!standalone);
      }
      return serverProcess;
    }

    internal void SendServerCommand(string inputText)
    {
      if (!ServerRunning)
      {
        return;
      }

      WriteToServer(inputText);
    }

    private void OnEndServer()
    {
      ServerExited?.Invoke(serverProcess, new EventArgs());
      isEmbedded = false;
    }

    private void ServerProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
    {
      ServerDataReceived?.Invoke(sender, e);
    }

    private Process StartSubnautica()
    {
      string subnauticaExe = Path.Combine(subnauticaPath, "Subnautica.exe");
      ProcessStartInfo startInfo = new ProcessStartInfo
      {
        WorkingDirectory = subnauticaPath,
        FileName = subnauticaExe
      };

      if (!PirateDetection.HasTriggered)
      {
        if (PlatformDetection.IsEpic(subnauticaPath))
        {
          startInfo.Arguments = "-EpicPortal -vrmode none";
        }
        else if (PlatformDetection.IsSteam(subnauticaPath))
        {
          startInfo.FileName = "steam://run/264710";
        }
        else if (PlatformDetection.IsMicrosoftStore(subnauticaPath))
        {
          startInfo.FileName = "ms-xbl-38616e6e:\\";
        }
      }
      return Process.Start(startInfo);
    }

    private void OnStartServer(bool embedded)
    {
      isEmbedded = embedded;
      ServerStarted?.Invoke(serverProcess, new ServerStartEventArgs(embedded));
    }

    private void OnSubnauticaExited(object sender, EventArgs e)
    {
      try
      {
        nitroxEntryPatch.Remove();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
      Log.Info("Finished removing patches!");
    }

    private async Task<Process> WaitForProcessAsync()
    {
      if (gameProcess != null)
      {
        return gameProcess;
      }

      return await Task.Run(async () =>
          {
            int waitTimeInMill = 1000;
            for (int i = 0; i < 1000; i++)
            {
              Process[] processes = Process.GetProcessesByName("Subnautica");
              if (processes.Length == 1)
              {
                return processes[0];
              }

              await Task.Delay(waitTimeInMill);
            }
            if (gameProcess == null)
            {
              Log.Error("No or multiple subnautica processes found. Cannot remove patches after exited.");
            }
            return null;
          })
          .ContinueWith(task =>
              {
                Process proc = task.Result;
                proc.Exited += OnSubnauticaExited;
                return proc;
              },
              TaskContinuationOptions.OnlyOnRanToCompletion);
    }
  }
}
