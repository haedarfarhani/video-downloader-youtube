using VideoDownloader.WinForms;

namespace VideoDownloader.WinForms
{
    public static class FormatSelector
    {
        public static string SelectFormat(string formatId)
        {
            return formatId?.ToLower() switch
            {
                "mp4" => "Video (MP4)",
                "webm" => "Video (WebM)",
                "mp3" => "Audio (MP3)",
                "m4a" => "Audio (M4A)",
                "flac" => "Audio (FLAC)",
                "wav" => "Audio (WAV)",
                _ => "Unknown"
            };
        }

        public static string GetQuality(string formatId, string quality)
        {
            if (string.IsNullOrEmpty(quality)) return "Standard";

            return formatId?.ToLower() switch
            {
                "mp3" or "m4a" or "flac" or "wav" => quality + " kbps",
                _ => quality
            };
        }
    }
}