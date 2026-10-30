namespace VideoDownloader.Services;

public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message, string confirmText = "Yes", string cancelText = "No", CancellationToken ct = default);
    Task ShowMessageAsync(string title, string message, CancellationToken ct = default);
}

public sealed class DialogService : IDialogService
{
    public Task<bool> ConfirmAsync(string title, string message, string confirmText = "Yes", string cancelText = "No", CancellationToken ct = default)
    {
        // Use ContentDialog or message box integration; stub returns false for safety
        return Task.FromResult(false);
    }

    public Task ShowMessageAsync(string title, string message, CancellationToken ct = default)
        => Task.CompletedTask;
}
