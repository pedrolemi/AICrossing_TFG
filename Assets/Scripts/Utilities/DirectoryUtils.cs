using System.IO;

namespace Utilities
{
    public static class DirectoryUtils
    {
        public static bool CopyDirectory(string sourcePath, string destPath)
        {
            if (!Directory.Exists(sourcePath))
            {
                return false;
            }

            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            string[] files = Directory.GetFiles(sourcePath);
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                if (!fileName.EndsWith(".meta"))
                {
                    string destFilePath = Path.Combine(destPath, fileName);
                    File.Copy(file, destFilePath, true);
                }
            }

            string[] dirs = Directory.GetDirectories(sourcePath);
            foreach (string dir in dirs)
            {
                string destDirPath = Path.Combine(destPath, Path.GetFileName(dir));
                CopyDirectory(dir, destDirPath);
            }

            return true;
        }
    }
}
