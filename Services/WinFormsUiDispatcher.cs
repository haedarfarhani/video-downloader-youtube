using System.Windows.Forms;

namespace VideoDownloader.Services;

/// <summary>
/// Marshals calls onto the WinForms UI thread.
/// Services are constructed while the container is being built (before any window exists),
/// so the target control is attached lazily. Work posted before a control is attached is
/// buffered and replayed on the UI thread once a control becomes available.
/// </summary>
public sealed class WinFormsUiDispatcher : IUiDispatcher
{
    private readonly object _gate = new();
    private readonly Queue<Action> _pending = new();
    private readonly int _uiThreadId;

    private Control? _target;

    public WinFormsUiDispatcher()
    {
        _uiThreadId = Environment.CurrentManagedThreadId;
    }

    public bool HasThreadAccess => Environment.CurrentManagedThreadId == _uiThreadId;

    /// <summary>
    /// Binds the dispatcher to the window whose thread owns the UI.
    /// Must be called on the UI thread (for example from <c>Control.OnHandleCreated</c>).
    /// </summary>
    public void Attach(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);

        Action[] pending;
        lock (_gate)
        {
            _target = control;
            pending = [.. _pending];
            _pending.Clear();
        }

        foreach (var action in pending)
        {
            action();
        }
    }

    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (HasThreadAccess)
        {
            action();
            return;
        }

        Control? target;
        lock (_gate)
        {
            target = _target;
            if (target is null)
            {
                _pending.Enqueue(action);
                return;
            }
        }

        if (target.IsDisposed)
        {
            return;
        }

        try
        {
            target.BeginInvoke(action);
        }
        catch (ObjectDisposedException)
        {
            // The window closed between the checks above; drop the notification.
        }
        catch (InvalidOperationException) when (!target.IsHandleCreated)
        {
            lock (_gate)
            {
                _pending.Enqueue(action);
            }
        }
    }
}
