using System.Diagnostics;

namespace VideoDownloader.WinForms
{
    public static class ProcessOutputTail
    {
        public static async Task<string> ReadLastNLinesAsync(
            string filePath, 
            int lines = 10)
        {
            if (!File.Exists(filePath)) return string.Empty;

            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(stream);

            var allLines = new List<string>();
            var buffer = new char[8192];
            int bytesRead;

            do
            {
                bytesRead = await reader.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    var text = new string(buffer, 0, bytesRead);
                    allLines.AddRange(text.Split('\n'));
                }
            } while (bytesRead > 0);

            var tailLines = allLines.Skip(Math.Max(0, allLines.Count - lines)).ToArray();
            return string.Join('\n', tailLines);
        }
    }
}