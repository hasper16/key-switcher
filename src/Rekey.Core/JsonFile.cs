using System.Globalization;

namespace Rekey.Core {
  // The file handling the stores share: a write that replaces the file in one step, and a way to
  // keep a file Rekey cannot read.
  internal static class JsonFile {
    public static void WriteAtomically(string path, Action<Stream> write) {
      Directory.CreateDirectory(Path.GetDirectoryName(path)!);
      string tempPath = path + ".tmp";
      using (FileStream stream = File.Create(tempPath)) {
        write(stream);

        // Reach the disk before the swap, so a power loss cannot leave an empty file.
        stream.Flush(flushToDisk: true);
      }

      if (File.Exists(path)) {
        File.Replace(tempPath, path, destinationBackupFileName: null);
      } else {
        File.Move(tempPath, path);
      }
    }

    public static void KeepUnreadable(string path) {
      try {
        File.Move(path, path + ".bak", overwrite: true);
      } catch (Exception e) when (e is IOException or UnauthorizedAccessException) {
        // The usual backup can be locked or read-only; a dated name still keeps the file safe from
        // what is written next.
        string stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
        File.Move(path, $"{path}.{stamp}.bak");
      }
    }
  }
}
