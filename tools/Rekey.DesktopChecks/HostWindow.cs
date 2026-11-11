using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Rekey.DesktopChecks {
  // A test window in its own process, so each has its own input thread and Layout: an ordinary
  // text box and a classic masked Win32 edit (ES_PASSWORD).
  internal sealed class HostWindow : IDisposable {
    private const string _command = "host";

    private readonly Process _process;

    private HostWindow(Process process, Point ordinary, Point masked) {
      _process = process;
      Ordinary = ordinary;
      Masked = masked;
    }

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
        var window = new HostWindow(process, Parse(points, 0), Parse(points, 2));
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
