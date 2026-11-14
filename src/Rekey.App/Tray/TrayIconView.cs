using System.Windows;
using System.Windows.Controls;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Win32;
using Rekey.App.ViewModels;
using Rekey.Win32;

namespace Rekey.App.Tray {
  // The notification-area icon and its menu, redrawn from the TrayViewModel whenever something
  // it shows may have changed.
  internal sealed class TrayIconView : IDisposable {
    private const string _themeKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private readonly TaskbarIcon _icon;
    private readonly TrayViewModel _model;
    private readonly TrayCommands _commands;
    private (string Code, TrayState State, bool Dark, int Size)? _drawn;

    public TrayIconView(TaskbarIcon icon, TrayViewModel model, TrayCommands commands) {
      _icon = icon;
      _model = model;
      _commands = commands;
      _icon.MenuActivation = PopupActivationMode.LeftOrRightClick;
      _icon.ContextMenu = new ContextMenu();
    }

    // For the desktop checks: the debug items the app adds go first.
    public IList<MenuItem> ExtraItems { get; } = [];

    public void Refresh() {
      _icon.ToolTipText = _model.ToolTip;
      DrawIcon();
      BuildMenu();
    }

    public MenuItem? FindItem(string header) {
      return _icon.ContextMenu.Items.OfType<MenuItem>()
          .FirstOrDefault(item => Equals(item.Header, header));
    }

    public void Dispose() {
      _icon.Dispose();
    }

    // The taskbar has its own light or dark setting, apart from apps.
    private static bool TaskbarIsDark() {
      using RegistryKey? key = Registry.CurrentUser.OpenSubKey(_themeKey);
      return key?.GetValue("SystemUsesLightTheme") is int light && light == 0;
    }

    // Statistics and Check for updates arrive with their own changes (proposal).
    private static MenuItem Disabled(string text) {
      return new MenuItem { Header = text, IsEnabled = false };
    }

    private void DrawIcon() {
      var look = (_model.LanguageCode ?? "··", _model.State, TaskbarIsDark(),
          Display.SmallIconSize());
      if (_drawn == look) {
        return;
      }

      _drawn = look;
      _icon.Icon = IconFile.From(Keycap.Render(look.Item1, look.State, look.Item3, look.Item4));
    }

    private void BuildMenu() {
      ItemCollection items = _icon.ContextMenu.Items;
      items.Clear();
      foreach (MenuItem extra in ExtraItems) {
        items.Add(extra);
      }

      MenuItem autoCorrection = Item(Strings.MenuAutoCorrection, _model.ToggleAutoCorrection);
      autoCorrection.IsCheckable = true;
      autoCorrection.IsChecked = _model.AutoCorrectionEnabled;
      items.Add(autoCorrection);
      items.Add(_model.IsPaused ? Item(Strings.MenuResume, _model.Resume) : PauseMenu());
      if (_model.ExcludeItemText is { } exclude) {
        items.Add(Item(exclude, _model.ToggleCurrentApp));
      }

      items.Add(new Separator());
      items.Add(Item(Strings.MenuSettings, _commands.OpenSettings));
      items.Add(Disabled(Strings.MenuStatistics));
      items.Add(Disabled(Strings.MenuCheckForUpdates));
      items.Add(Item(Strings.MenuAbout, _commands.OpenAbout));
      items.Add(new Separator());
      items.Add(Item(Strings.MenuQuit, _commands.Quit));
    }

    private MenuItem PauseMenu() {
      var pause = new MenuItem { Header = Strings.MenuPause };
      (string Text, PausePreset Preset)[] presets = [
          (Strings.MenuPause15, PausePreset.FifteenMinutes),
          (Strings.MenuPause30, PausePreset.ThirtyMinutes),
          (Strings.MenuPause60, PausePreset.OneHour),
          (Strings.MenuPause180, PausePreset.ThreeHours),
          (Strings.MenuPauseUntilRestart, PausePreset.UntilRestart)];
      foreach ((string text, PausePreset preset) in presets) {
        pause.Items.Add(Item(text, () => _model.Pause(preset)));
      }

      return pause;
    }

    private MenuItem Item(string text, Action action) {
      var item = new MenuItem { Header = text };
      item.Click += (_, _) => {
        action();
        Refresh();
      };
      return item;
    }
  }
}
