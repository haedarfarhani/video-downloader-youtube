

namespace VideoDownloader.WinForms {
    public class DownloadHistoryRecord
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
        public DateTime StartTime { get; set; } = DateTime.MinValue;
        public DateTime? EndTime { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public int DurationSeconds { get; set; } = 0;
    }
}


