using System.Text.RegularExpressions;

namespace VideoDownloader.WinForms
{
    public static class UrlValidator
    {
        private static readonly Regex _uriValidator = 
            new Regex(@"^(https?:\/\/)?([A-Za-z0-9-]+\.)+[A-Za-z]{2,}(\/|\?|\#)?$", 
                RegexOptions.Compiled | RegexOptions.CultureInvariant);

        public static bool IsValidUrl(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return false;
            
            input = input.Trim();
            if (input.StartsWith("http", StringComparison.OrdinalIgnoreCase) || 
                input.StartsWith("ftp", StringComparison.OrdinalIgnoreCase))
            {
                return _uriValidator.IsMatch(input);
            }
            
            return _uriValidator.IsMatch("http://" + input);
        }
    }
}