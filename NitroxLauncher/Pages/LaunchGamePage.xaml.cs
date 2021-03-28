using System;
using System.Windows;
using System.Windows.Navigation;

namespace NitroxLauncher.Pages
{
  public partial class LaunchGamePage : PageBase
  {
    private static App _app = Application.Current as App;

    public LaunchGamePage()
    {
      InitializeComponent();
    }

    private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
      System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
      e.Handled = true;
    }

    private async void SinglePlayerButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        await _app.LauncherLogic.StartSingleplayerAsync();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.ToString(), "Error while starting in singleplayer mode", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    private async void MultiplayerButton_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        await _app.LauncherLogic.StartMultiplayerAsync();
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.ToString(), "Error while starting in multiplayer mode", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}
