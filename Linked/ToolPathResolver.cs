using System.IO;
using System.Reflection;
using VideoDownloader.Configuration;

namespace VideoDownloader.WinForms
{
    public static class ToolPathResolver
    {
        public static string GetYtDlpPath()
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                ?? AppContext.BaseDirectory;
            return Path.Combine(basePath, "Tools", "yt-dlp", "yt-dlp.exe");
        }

        public static string GetFfmpegPath()
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                ?? AppContext.BaseDirectory;
            return Path.Combine(basePath, "Tools", "ffmpeg", "ffmpeg.exe");
        }

        public static string GetFfprobePath()
        {
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) 
                ?? AppContext.BaseDirectory;
            return Path.Combine(basePath, "Tools", "ffmpeg", "ffprobe.exe");
        }

        public static bool IsToolAvailable(string path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        public static string GetApplicationPath(AppPaths paths)
        {
            return paths.GetApplicationDirectory();
        }
    }
}