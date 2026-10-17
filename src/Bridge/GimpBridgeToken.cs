namespace Loupedeck.GimpPlugin
{
    using System;
    using System.IO;
    using System.Linq;

    // The token the GIMP bridge plug-in writes on every start (token_path() in loupedeck-bridge.py).
    // Read on every connect, so a restarted GIMP with a new token is picked up.
    internal static class GimpBridgeToken
    {
        private const Int32 Length = 64;

        public static String Read()
        {
            var baseDir = OperatingSystem.IsWindows()
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support");
            var path = Path.Combine(baseDir, "loupedeck-gimp-bridge", "token");

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var buffer = new Char[Length + 1];
            var token = new String(buffer, 0, new StreamReader(stream).ReadBlock(buffer)).Trim();
            return token.Length == Length && token.All(Uri.IsHexDigit) ? token : throw new IOException("invalid bridge token");
        }
    }
}
