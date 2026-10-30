namespace VideoDownloader.Services;

public interface IUiDispatcher
{
    bool HasThreadAccess { get; }
    void Post(Action action);
}
