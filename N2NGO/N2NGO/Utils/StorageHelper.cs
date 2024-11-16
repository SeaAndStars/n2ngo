using System.IO;
using System;

namespace N2NGO.Utils;

internal static class StorageHelper
{
    public static long? GetDirectorySize(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            return null;

        long size = 0;

        DirectoryInfo directoryRoot = new(dirPath);
        DirectoryInfo[] directories = directoryRoot.GetDirectories();

        foreach (FileInfo file in directoryRoot.GetFiles())
            size += file.Length;

        foreach (DirectoryInfo directory in directories)
            size += GetDirectorySize(directory.FullName) ?? throw new($"Directory {directory.FullName} not exists");

        return size;
    }

    public static string GetFileSizeReadableString(long size)
    {
        const int scale = 1024;

        if (size < scale)
            return $"{size}B";
        if (size < Math.Pow(scale, 2))
            return $"{size / scale:f2}KB";
        if (size < Math.Pow(scale, 3))
            return $"{size / Math.Pow(scale, 2):f2}MB";
        if (size < Math.Pow(scale, 4))
            return $"{size / Math.Pow(scale, 3):f2}GB";

        return $"{size / Math.Pow(scale, 4):f2}TB";
    }
}
