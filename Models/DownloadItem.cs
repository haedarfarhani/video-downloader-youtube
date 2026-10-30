using VideoDownloader.WinForms;

namespace VideoDownloader.WinForms {
    public class DownloadItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Downloading, Completed, Failed
        public string Progress { get; set; } = "0%";
        public DateTime StartTime { get; set; } = DateTime.MinValue;
        public DateTime? EndTime { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}


