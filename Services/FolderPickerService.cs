namespace VideoDownloader.WinForms.Services;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}

public class FolderPickerService : IFolderPickerService
{
    public Task<string?> PickFolderAsync()
    {
        using var dlg = new FolderBrowserDialog();
        dlg.Description = "Select folder";
        return dlg.ShowDialog() == DialogResult.OK ? Task.FromResult<string?>(dlg.SelectedPath) : Task.FromResult<string?>(null);
    }
}