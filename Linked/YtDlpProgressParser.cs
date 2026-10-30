using System.Text.RegularExpressions;
using VideoDownloader.WinForms;

namespace VideoDownloader.WinForms
{
    public static class YtDlpProgressParser
    {
        private static readonly Regex ProgressRegex = new Regex(
            @"\[download\]\s+(?<percent>\d+(?:\.\d+)?)%\s+of\s+(?<size>[\d\.]+\s*\w+)\s+at\s+(?<speed>[\d\.]+\s*\w+/s)\s+ETA\s+(?<eta>[\d:]+)",
            RegexOptions.Compiled);

        private static readonly Regex FileNameRegex = new Regex(
            @"\[download\]\s+Destination:\s+(?<filename>.+)",
            RegexOptions.Compiled);

        public static bool TryParseProgress(string line, out double percent, out string size, out string speed, out string eta)
        {
            percent = 0;
            size = string.Empty;
            speed = string.Empty;
            eta = string.Empty;

            var match = ProgressRegex.Match(line);
            if (match.Success)
            {
                double.TryParse(match.Groups["percent"].Value, out percent);
                size = match.Groups["size"].Value;
                speed = match.Groups["speed"].Value;
                eta = match.Groups["eta"].Value;
                return true;
            }
            return false;
        }

        public static bool TryParseFileName(string line, out string filename)
        {
            filename = string.Empty;
            var match = FileNameRegex.Match(line);
            if (match.Success)
            {
                filename = match.Groups["filename"].Value;
                return true;
            }
            return false;
        }
    }
}