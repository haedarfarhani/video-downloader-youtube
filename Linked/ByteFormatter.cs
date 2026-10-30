namespace VideoDownloader.WinForms
{
    public static class ByteFormatter
    {
        private static readonly string[] Sizes = { "B", "KB", "MB", "GB", "TB" };

        public static string Format(long bytes)
        {
            if (bytes < 0) return "-" + Format(-bytes);
            
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < Sizes.Length - 1)
            {
                len = len / 1024;
                order++;
            }

            return $"{len:0.##} {Sizes[order]}";
        }
    }
}