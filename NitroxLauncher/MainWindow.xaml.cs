using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using NitroxLauncher.AttachedProperties;
using NitroxLauncher.Events;
using NitroxLauncher.Pages;
using NitroxModel.Discovery;

namespace NitroxLauncher
{
  public partial class MainWindow : Window, INotifyPropertyChanged
  {
    private static App _app => Application.Current as App;

    private bool isServerEmbedded;

    public string Version => $"{LauncherLogic.RELEASE_PHASE} {LauncherLogic.Version}";

    private Page _currentPage
    {
      get => MainFrame.Content as Page;
      set
      {
        MainFrame.Content = value;
      }
    }

    public MainWindow()
    {
      InitializeComponent();

      _app.LauncherLogic.ServerStarted += ServerStarted;
      _app.LauncherLogic.ServerExited += ServerExited;

      _app.LauncherLogic.SetTargetedSubnauticaPath(GameInstallationFinder.Instance.FindGame()).ContinueWith(task =>
      {
        if (_app.LauncherLogic.IsSubnauticaDirectory(task.Result))
        {
          _currentPage = _app.LaunchGamePage;
        }
        else
        {
          _currentPage = _app.OptionPage;
        }

      },
      CancellationToken.None,
      TaskContinuationOptions.OnlyOnRanToCompletion,
      TaskScheduler.FromCurrentSynchronizationContext());
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool CanClose()
    {
      if (_app.LauncherLogic.ServerRunning && isServerEmbedded)
      {
        MessageBoxResult userAnswer = MessageBox.Show("The embedded server is still running. Click yes to close.", "Close aborted", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        return userAnswer == MessageBoxResult.Yes;
      }

      return true;
    }

    private void OnClosing(object sender, CancelEventArgs e)
    {
      if (!CanClose())
      {
        e.Cancel = true;
        return;
      }
      _app.LauncherLogic.Dispose();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }

    private void Minimze_Click(object sender, RoutedEventArgs e)
    {
      WindowState = WindowState.Minimized;
    }

    private void ServerStarted(object sender, ServerStartEventArgs e)
    {
      isServerEmbedded = e.Embedded;

      if (e.Embedded)
      {
        _currentPage = _app.ServerConsolePage;
      }
    }

    private void ServerExited(object sender, EventArgs e)
    {
      Dispatcher?.Invoke(() =>
      {
        if (_currentPage is ServerConsolePage)
        {
          _currentPage = _app.ServerPage;
        }
      });
    }

    private void LaunchGamePageButton_Click(object sender, RoutedEventArgs e)
    {
      _currentPage = _app.LaunchGamePage;
    }
    private void ServerPageButton_Click(object sender, RoutedEventArgs e)
    {
      _currentPage = _app.ServerPage;
    }
    private void OptionPageButton_Click(object sender, RoutedEventArgs e)
    {
      _currentPage = _app.OptionPage;
    }
  }
}
