using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NitroxLauncher.Pages;
using NitroxModel.Logger;

namespace NitroxLauncher
{
  public partial class App : Application
  {
    public LaunchGamePage LaunchGamePage { get; private set; }
    public OptionPage OptionPage { get; private set; }
    public ServerConsolePage ServerConsolePage { get; private set; }
    public ServerPage ServerPage { get; private set; }
    public LauncherLogic LauncherLogic { get; private set; }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
      LauncherLogic = new LauncherLogic();
      LaunchGamePage = new LaunchGamePage();
      OptionPage = new OptionPage();
      ServerConsolePage = new ServerConsolePage();
      ServerPage = new ServerPage();
      MainWindow = new MainWindow();
      MainWindow.Show();
      

      Log.Setup();

      // Error if running from a temporary directory because Nitrox Launcher won't be able to write files directly to zip/rar
      // Tools like WinRAR do this to support running EXE files while it's still zipped.
      if (Directory.GetCurrentDirectory().StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
      {
        MessageBox.Show("Nitrox launcher should not be executed from a temporary directory. Install Nitrox launcher properly by extracting ALL files and moving these to a dedicated location on your PC.",
          "Invalid working directory",
          MessageBoxButton.OK,
          MessageBoxImage.Error);
        Environment.Exit(1);
      }
    }

    private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
      // If something went wrong. Close the server if embedded.
      LauncherLogic.Dispose();
      Log.Error(e.Exception.GetBaseException().ToString()); // Gets the exception that was unhandled, not the "dispatched unhandled" exception.
      MessageBox.Show(e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
  }
}
