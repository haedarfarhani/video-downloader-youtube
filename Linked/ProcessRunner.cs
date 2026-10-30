using System.Diagnostics;
using System.Threading.Tasks;
using VideoDownloader.Configuration;

namespace VideoDownloader.WinForms
{
    public static class ProcessRunner
    {
        public static async Task<(int ExitCode, string Output, string Error)> RunAsync(
            string arguments, 
            string workingDirectory = null,
            bool useShellExecute = false)
        {
            var ytDlpPath = ToolPathResolver.GetYtDlpPath();
            
            var startInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = arguments,
                UseShellExecute = useShellExecute,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            using var process = Process.Start(startInfo);
            if (process == null) return (-1, string.Empty, string.Empty);

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var exitCode = process.ExitCode;
            var output = await outputTask;
            var error = await errorTask;

            return (exitCode, output, error);
        }

        public static async Task RunWithProgressAsync(
            string arguments,
            IProgress<ProgressReport> progress,
            string workingDirectory = null)
        {
            var ytDlpPath = ToolPathResolver.GetYtDlpPath();
            
            var startInfo = new ProcessStartInfo
            {
                FileName = ytDlpPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            using var process = Process.Start(startInfo);
            if (process == null) return;

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            await process.WaitForExitAsync();

            // Parse progress from output
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                var parsed = YtDlpProgressParser.TryParseProgress(line, 
                    out var percent, out var size, out var speed, out var eta);
                if (parsed)
                {
                    progress.Report(new ProgressReport
                    {
                        Percent = (int)percent,
                        Size = size,
                        Speed = speed,
                        Eta = eta,
                        IsComplete = percent >= 100
                    });
                }
            }
        }
    }
}