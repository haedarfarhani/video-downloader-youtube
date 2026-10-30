

namespace VideoDownloader.WinForms {
    public class ConversionOptions
    {
        public string OutputFormat { get; set; } = "mp4";
        public string Quality { get; set; } = "Best";
        public bool IncludeSubtitles { get; set; } = false;
        public bool IncludeThumbnails { get; set; } = false;
        public bool KeepOriginal { get; set; } = false;
        public string Preset { get; set; } = "High";
    }
}


