using Windows.Storage.Pickers;
using WinRT.Interop;

namespace VideoDownloader.Services;

/// <summary>Opens the Windows folder picker on behalf of the view models.</summary>
public interface IFolderPickerService
{
    Task<string?> PickFolderAsync(CancellationToken cancellationToken = default);
}

public sealed class FolderPickerService : IFolderPickerService
{
    private readonly Func<Microsoft.UI.Xaml.Window?> _windowAccessor;

    public FolderPickerService(Func<Microsoft.UI.Xaml.Window?> windowAccessor)
    {
        _windowAccessor = windowAccessor;
    }

    public async Task<string?> PickFolderAsync(CancellationToken cancellationToken = default)
    {
        var window = _windowAccessor();
        if (window is null)
        {
            return null;
        }

        var picker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.Downloads
        };

        // FolderPicker requires at least one filter even though it does not display files.
        picker.FileTypeFilter.Add("*");

        var handle = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(picker, handle);

        var folder = await picker.PickSingleFolderAsync().AsTask(cancellationToken);
        return folder?.Path;
    }
}
