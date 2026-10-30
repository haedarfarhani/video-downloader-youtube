using VideoDownloader.WinForms;

namespace VideoDownloader.WinForms
{
    public static class YtDlpArgumentBuilder
    {
        public static string BuildDownloadArguments(string url, string outputPath, string format, bool audioOnly)
        {
            var args = new List<string>();
            
            args.Add("https://www.youtube.com/watch?v=" + url);
            args.Add("--no-warnings");
            args.Add("--progress");
            args.Add("--write-info-json");
            args.Add("--output");
            args.Add('"' + outputPath + '"');
            
            if (audioOnly)
            {
                args.Add("--extract-audio");
                args.Add("--audio-format");
                args.Add("mp3");
            }
            else
            {
                args.Add("--merge-output-format");
                args.Add(format.ToLower());
            }
            
            return string.Join(" ", args);
        }
    }
}