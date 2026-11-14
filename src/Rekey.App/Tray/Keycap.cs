using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Rekey.App.ViewModels;

namespace Rekey.App.Tray {
  // Rekey's mark: a keyboard key with the Language code on it (design D1). The tray icon and
  // the cursor indicator draw it at the pixel size they need, so the legend stays crisp.
  internal static class Keycap {
    private static readonly Color _lightFace = Color.FromRgb(0xFA, 0xFA, 0xFA);
    private static readonly Color _lightEdge = Color.FromRgb(0xC9, 0xCD, 0xD3);
    private static readonly Color _lightLegend = Color.FromRgb(0x1F, 0x23, 0x28);
    private static readonly Color _darkFace = Color.FromRgb(0x2D, 0x30, 0x36);
    private static readonly Color _darkEdge = Color.FromRgb(0x4A, 0x4F, 0x57);
    private static readonly Color _darkLegend = Color.FromRgb(0xF2, 0xF4, 0xF7);
    private static readonly Color _pausedEdge = Color.FromRgb(0xC9, 0x8A, 0x00);
    private static readonly Color _selfPausedEdge = Color.FromRgb(0xC4, 0x2B, 0x1C);
    private static readonly Typeface _legendFace = new(
        new FontFamily("Segoe UI Variable Display, Segoe UI"),
        FontStyles.Normal,
        FontWeights.SemiBold,
        FontStretches.Normal);

    public static BitmapSource Render(string code, TrayState state, bool dark, int size) {
      var visual = new DrawingVisual();
      using (DrawingContext context = visual.RenderOpen()) {
        Draw(context, code, state, dark, size, new Point(0, 0));
      }

      var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
      bitmap.Render(visual);
      bitmap.Freeze();
      return bitmap;
    }

    // Every state in both themes, one column per size, on a background like the taskbar's.
    public static void WriteSheet(string path, IReadOnlyList<int> sizes) {
      const int gap = 8;
      TrayState[] states = Enum.GetValues<TrayState>();
      int cell = sizes.Max() + gap;
      int width = (cell * sizes.Count) + gap;
      int height = (cell * states.Length * 2) + gap;
      var visual = new DrawingVisual();
      using (DrawingContext context = visual.RenderOpen()) {
        int row = 0;
        foreach (bool dark in new[] { false, true }) {
          foreach (TrayState state in states) {
            double top = gap + (row * cell);
            var band = new Rect(0, top - (gap / 2), width, cell);
            context.DrawRectangle(
                new SolidColorBrush(dark ? Color.FromRgb(0x20, 0x20, 0x20) : Colors.White),
                null,
                band);
            for (int column = 0; column < sizes.Count; column++) {
              var at = new Point(gap + (column * cell), top);
              Draw(context, "EN", state, dark, sizes[column], at);
            }

            row++;
          }
        }
      }

      var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
      bitmap.Render(visual);
      var encoder = new PngBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(bitmap));
      using FileStream file = File.Create(path);
      encoder.Save(file);
    }

    private static void Draw(
        DrawingContext context, string code, TrayState state, bool dark, int size, Point at) {
      Color edge = state switch {
        TrayState.Paused => _pausedEdge,
        TrayState.SelfPaused => _selfPausedEdge,
        _ => dark ? _darkEdge : _lightEdge,
      };
      double radius = size * 0.2;
      var body = new Rect(at.X, at.Y, size, size);
      context.DrawRoundedRectangle(Brush(edge), null, body, radius, radius);

      // A taller bottom edge makes the square read as a key seen from slightly above.
      double side = Math.Max(1, Math.Round(size * 0.07));
      double bottom = Math.Max(2, Math.Round(size * 0.17));
      var face = new Rect(at.X + side, at.Y + side, size - (2 * side), size - side - bottom);
      context.DrawRoundedRectangle(
          Brush(dark ? _darkFace : _lightFace), null, face, radius * 0.7, radius * 0.7);

      Color legend = dark ? _darkLegend : _lightLegend;
      var text = new FormattedText(
          code,
          CultureInfo.InvariantCulture,
          FlowDirection.LeftToRight,
          _legendFace,
          LegendSize(code, face.Width),
          Brush(legend, state == TrayState.Off ? 0.45 : 1),
          1.0);
      context.DrawText(
          text,
          new Point(
              face.X + ((face.Width - text.Width) / 2),
              face.Y + ((face.Height - text.Height) / 2)));

      if (state == TrayState.SelfPaused) {
        double dot = Math.Max(1.5, size * 0.13);
        context.DrawEllipse(
            Brush(_selfPausedEdge),
            new Pen(Brush(dark ? _darkFace : _lightFace), Math.Max(1, size * 0.04)),
            new Point(at.X + size - dot, at.Y + dot),
            dot,
            dot);
      }
    }

    // Two letters fill most of the face; a longer code shrinks to fit.
    private static double LegendSize(string code, double faceWidth) {
      return faceWidth * (code.Length <= 2 ? 0.52 : 1.05 / code.Length);
    }

    private static SolidColorBrush Brush(Color color, double opacity = 1) {
      var brush = new SolidColorBrush(color) { Opacity = opacity };
      brush.Freeze();
      return brush;
    }
  }
}
