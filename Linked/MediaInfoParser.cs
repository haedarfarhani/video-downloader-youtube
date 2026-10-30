using System.Collections.Generic;
using System.Text.RegularExpressions;
using VideoDownloader.Models;

namespace VideoDownloader.WinForms
{
    public static class MediaInfoParser
    {
        public static MediaInfo ParseMediaInfo(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
                return new MediaInfo();

            // Use MediaInfo library or ffprobe - stub implementation
            return new MediaInfo
            {
                FilePath = filePath,
                FileName = System.IO.Path.GetFileName(filePath),
                FileSize = System.IO.File.Exists(filePath) 
                    ? new System.IO.FileInfo(filePath).Length 
                    : 0,
                Duration = 0,
                VideoCodec = "Unknown",
                AudioCodec = "Unknown"
            };
        }

        public static MediaInfo ParseMediaInfoFromConsoleOutput(string output)
        {
            var info = new MediaInfo();

            var durationMatch = Regex.Match(output, @"Duration:\s+(\d+:\d+:\d+)");
            if (durationMatch.Success)
            {
                // Parse duration
            }

            var sizeMatch = Regex.Match(output, @"(\d+(?:\.\d+)?)\s+MiB");
            if (sizeMatch.Success)
            {
                info.FileSize = (long)(double.Parse(sizeMatch.Groups[1].Value) * 1024 * 1024);
            }

            return info;
        }
    }
}