using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace NitroxLauncher.Pages
{
  public partial class OptionPage : PageBase, INotifyPropertyChanged
  {
    private static App _app = Application.Current as App;

    public string PathToSubnautica => _app.LauncherLogic.SubnauticaPath;

    public OptionPage()
    {
      InitializeComponent();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private async void ChangeOptions_Click(object sender, RoutedEventArgs e)
    {
      string selectedDirectory;

      // Don't use FolderBrowserDialog because its UI sucks. See: https://stackoverflow.com/a/31082
      CommonOpenFileDialog dialog = new CommonOpenFileDialog
      {
        Multiselect = false,
        InitialDirectory = PathToSubnautica,
        EnsurePathExists = true,
        IsFolderPicker = true,
        Title = "Select Subnautica installation directory"
      };
      using (dialog)
      {
        if (dialog.ShowDialog() != CommonFileDialogResult.Ok)
        {
          return;
        }
        selectedDirectory = Path.GetFullPath(dialog.FileName);
      }

      if (_app.LauncherLogic.IsSubnauticaDirectory(selectedDirectory))
      {
        await _app.LauncherLogic.SetTargetedSubnauticaPath(selectedDirectory);
      }
      else
      {
        MessageBox.Show("The selected directory does not contain the required Subnautica.exe file.", "Invalid Subnautica directory", MessageBoxButton.OK, MessageBoxImage.Warning);
      }
    }

    private void OptionPage_OnLoaded(object sender, RoutedEventArgs e)
    {
      _app.LauncherLogic.PropertyChanged += OnLogicPropertyChanged;
      OnPropertyChanged(nameof(PathToSubnautica));
    }

    private void OptionPage_OnUnloaded(object sender, RoutedEventArgs e)
    {
      _app.LauncherLogic.PropertyChanged -= OnLogicPropertyChanged;
    }

    private void OnLogicPropertyChanged(object sender, PropertyChangedEventArgs args)
    {
      // Pass-through property change events.
      if (args.PropertyName == nameof(_app.LauncherLogic.SubnauticaPath))
      {
        OnPropertyChanged(nameof(PathToSubnautica));
      }
    }
  }
}
