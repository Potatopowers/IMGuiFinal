using System;
using System.Drawing;
using System.IO;

namespace ProfileApp
{
    internal static class ProfileImageHelper
    {
        public static string SaveProfileImageCopy(string sourcePath, string username, string profileKey)
        {
            var ext = Path.GetExtension(sourcePath);
            var destDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProfileImages");
            Directory.CreateDirectory(destDir);
            var destFile = Path.Combine(destDir, $"{username}_{profileKey}{ext}");
            File.Copy(sourcePath, destFile, overwrite: true);
            return destFile;
        }

        public static Image LoadImageClone(string path)
        {
            using var tmp = Image.FromFile(path);
            return new Bitmap(tmp); // clone to avoid file lock
        }
    }
}
