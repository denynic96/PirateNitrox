using System;
using System.Windows;
using System.Windows.Navigation;
using NitroxLauncher.Properties;

namespace NitroxLauncher.Pages
{
  public partial class ServerPage : PageBase
  {
    private static App _app = Application.Current as App;

    public string StartButtonSubtitle => $"NITROX {LauncherLogic.RELEASE_PHASE} {LauncherLogic.Version}";

    public ServerPage()
    {
      InitializeComponent();

      RBIsDocked.IsChecked = !Settings.Default.IsExternalServer;
      RBIsExternal.IsChecked = Settings.Default.IsExternalServer;
    }

    private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
      System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
      e.Handled = true;
    }

    private void StartServer_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        _app.LauncherLogic.StartServer(RBIsExternal.IsChecked == true);
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private void RBServer_Clicked(object sender, RoutedEventArgs e)
    {
      Settings.Default.IsExternalServer = RBIsExternal.IsChecked == true;
      Settings.Default.Save();
    }
  }
}
