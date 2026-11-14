using System.Drawing;
using System.IO;
using System.Windows.Media.Imaging;

namespace Rekey.App.Tray {
  // Wraps a rendered keycap in a one-image .ico, which Windows accepts with a PNG inside.
  internal static class IconFile {
    public static Icon From(BitmapSource image) {
      var encoder = new PngBitmapEncoder();
      encoder.Frames.Add(BitmapFrame.Create(image));
      using var png = new MemoryStream();
      encoder.Save(png);

      using var ico = new MemoryStream();
      using (var writer = new BinaryWriter(ico, System.Text.Encoding.UTF8, leaveOpen: true)) {
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)1);

        // A side of 256 is written as 0.
        writer.Write((byte)(image.PixelWidth % 256));
        writer.Write((byte)(image.PixelHeight % 256));
        writer.Write((byte)0);
        writer.Write((byte)0);
        writer.Write((ushort)1);
        writer.Write((ushort)32);
        writer.Write((uint)png.Length);
        writer.Write(22u);
        writer.Write(png.ToArray());
      }

      ico.Position = 0;
      return new Icon(ico);
    }
  }
}
