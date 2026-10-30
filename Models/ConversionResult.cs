

namespace VideoDownloader.WinForms {
    public class ConversionResult
    {
        public bool Success { get; set; } = false;
        public string OutputPath { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public long FileSize { get; set; } = 0;
        public TimeSpan Duration { get; set; } = TimeSpan.Zero;
        public string? StatusMessage { get; set; }
    }
}


