namespace VideoDownloader.Services;

public interface INotificationService
{
    event EventHandler<string>? Completed;
    event EventHandler<string>? Failed;
    void NotifyCompleted(string message);
    void NotifyFailed(string message);
}

public sealed class NotificationService : INotificationService
{
    public event EventHandler<string>? Completed;
    public event EventHandler<string>? Failed;

    public void NotifyCompleted(string message) => Completed?.Invoke(this, message);
    public void NotifyFailed(string message) => Failed?.Invoke(this, message);
}
