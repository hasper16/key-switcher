using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using Windows.Win32.Foundation;

namespace Rekey.DesktopChecks {
  // A test window in its own process, so each has its own input thread and Layout: an ordinary
  // text box and a classic masked Win32 edit (ES_PASSWORD).
  internal sealed class HostWindow : IDisposable {
    private const string _command = "host";

    // The window counts the Ctrl+Z presses that reach it in its title: "<title> z<count>".
    private const string _undoMark = " z";

    private readonly Process _process;

    private HostWindow(Process process, string title, Point ordinary, Point masked) {
      _process = process;
      Title = title;
      Ordinary = ordinary;
      Masked = masked;
    }

    public string Title { get; }

    public Point Ordinary { get; }

    public Point Masked { get; }

    public static bool IsHostCommand(string[] args) {
      return args.Length == 2 && args[0] == _command;
    }

    public static HostWindow Start(string title, Point location) {
      var start = new ProcessStartInfo(Environment.ProcessPath!) {
        RedirectStandardOutput = true,
        UseShellExecute = false,
      };
      start.ArgumentList.Add(_command);
      start.ArgumentList.Add(
          string.Create(CultureInfo.InvariantCulture, $"{title}|{location.X}|{location.Y}"));
      Process? process = Process.Start(start)
          ?? throw new InvalidOperationException("The test window did not start.");
      try {
        Task<string?> line = process.StandardOutput.ReadLineAsync();
        if (!line.Wait(TimeSpan.FromSeconds(15)) || line.Result is null) {
          throw new TimeoutException("The test window did not show within 15 s.");
        }

        string[] points = line.Result.Split(' ');
        var window = new HostWindow(process, title, Parse(points, 0), Parse(points, 2));
        process = null;
        return window;
      } finally {
        // Whatever failed before the hand-over, the child window must not outlive the checks.
        if (process is not null) {
          if (!process.HasExited) {
            process.Kill();
          }

          process.Dispose();
        }
      }
    }

    // Runs in the child process: shows the window and prints the boxes' screen centers.
    public static int Run(string spec) {
      string[] parts = spec.Split('|');
      Application.EnableVisualStyles();
      using var form = new Form {
        Text = parts[0],
        StartPosition = FormStartPosition.Manual,
        Location = new Point(
            int.Parse(parts[1], CultureInfo.InvariantCulture),
            int.Parse(parts[2], CultureInfo.InvariantCulture)),
        Size = new Size(420, 200),
        TopMost = true,
        KeyPreview = true,
      };
      int undos = 0;
      form.KeyDown += (_, key) => {
        if (key.Control && key.KeyCode == Keys.Z) {
          undos++;
          form.Text = string.Create(
              CultureInfo.InvariantCulture, $"{parts[0]}{_undoMark}{undos}");
        }
      };
      var ordinary = new TextBox { Location = new Point(20, 20), Width = 360 };
      var masked = new TextBox {
        Location = new Point(20, 80),
        Width = 360,
        UseSystemPasswordChar = true,
      };
      form.Controls.Add(ordinary);
      form.Controls.Add(masked);
      form.Shown += (_, _) => {
        Point first = Center(ordinary);
        Point second = Center(masked);
        Console.WriteLine(string.Create(
            CultureInfo.InvariantCulture, $"{first.X} {first.Y} {second.X} {second.Y}"));
        Console.Out.Flush();
      };
      Application.Run(form);
      return 0;
    }

    // The number of Ctrl+Z presses that reached the window.
    public int UndoKeysSeen() {
      HWND? window = Desktop.FindWindow(Title, TimeSpan.FromSeconds(1));
      string title = window is { } found ? Desktop.TitleOf(found) : Title;
      int mark = title.LastIndexOf(_undoMark, StringComparison.Ordinal);
      return mark < Title.Length
          ? 0
          : int.Parse(title[(mark + _undoMark.Length)..], CultureInfo.InvariantCulture);
    }

    public void Dispose() {
      if (!_process.HasExited) {
        _process.Kill();
      }

      _process.Dispose();
    }

    private static Point Center(Control control) {
      return control.PointToScreen(new Point(control.Width / 2, control.Height / 2));
    }

    private static Point Parse(string[] parts, int index) {
      return new Point(
          int.Parse(parts[index], CultureInfo.InvariantCulture),
          int.Parse(parts[index + 1], CultureInfo.InvariantCulture));
    }
  }
}
