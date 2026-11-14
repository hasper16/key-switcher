namespace Rekey.App.Tray {
  // What the tray menu asks of the app beyond the settings it changes itself.
  internal sealed record TrayCommands(Action OpenSettings, Action OpenAbout, Action Quit);
}
