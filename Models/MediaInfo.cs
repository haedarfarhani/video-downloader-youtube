using VideoDownloader.WinForms;

namespace VideoDownloader.WinForms {
    public class MediaInfo
    {
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; } = 0;
        public double Duration { get; set; } = 0; // in seconds
        public string VideoCodec { get; set; } = "Unknown";
        public string AudioCodec { get; set; } = "Unknown";
        public string ContainerFormat { get; set; } = "Unknown";
    }
}

