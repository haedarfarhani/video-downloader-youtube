using Microsoft.Extensions.DependencyInjection;
using VideoDownloader.Models;
using VideoDownloader.Services;
using VideoDownloader.WinForms.Controls;
using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Forms;

public partial class DashboardForm : ThemedPage
{
    private readonly IDownloadQueueService _queue;
    private readonly IDownloadHistoryService _history;
    private readonly IToolValidationService _tools;

    private readonly PageHeader _header;
    private readonly StatCard _activeCard;
    private readonly StatCard _pendingCard;
    private readonly StatCard _completedCard;
    private readonly StatCard _failedCard;

    private readonly CardPanel _activityCard;
    private readonly Label _activityTitle;
    private readonly Label _activityMeta;
    private readonly Label _activityEmpty;
    private readonly FlowLayoutPanel _activityRows;
    private readonly AccentButton _openHistory;

    private readonly CardPanel _toolsCard;
    private readonly Label _toolsTitle;
    private readonly Label _toolsSummary;
    private readonly TableLayoutPanel _toolGrid;
    private readonly AccentButton _validateTools;

    private readonly List<(Label Dot, Label Name, Label Status)> _toolRows = [];

    public DashboardForm(IServiceProvider provider, IThemeService themeService)
        : base(themeService)
    {
        _queue = provider.GetRequiredService<IDownloadQueueService>();
        _history = provider.GetRequiredService<IDownloadHistoryService>();
        _tools = provider.GetRequiredService<IToolValidationService>();

        _header = new PageHeader("Dashboard", "Everything happening in your download workspace.");

        _activeCard = new StatCard("Active downloads", "\uE896");
        _pendingCard = new StatCard("Waiting", "\uE823");
        _completedCard = new StatCard("Completed", "\uE73E");
        _failedCard = new StatCard("Failed", "\uE7BA");

        _activityTitle = new Label { Text = "Recent activity", Font = Ui.Heading, AutoSize = true };
        _activityMeta = new Label { Text = "Nothing downloaded yet", Font = Ui.Caption, AutoSize = true };
        _activityEmpty = new Label
        {
            Text = "Your finished downloads will appear here.\nStart with a link on the Downloads page.",
            Font = Ui.Body,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };

        _activityRows = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Dock = DockStyle.Fill
        };

        _openHistory = new AccentButton { Text = "Open history", Variant = ButtonVariant.Secondary, Width = 130 };
        _openHistory.Click += (_, _) => NavigateTo?.Invoke("History");

        _activityCard = BuildActivityCard();

        _toolsTitle = new Label { Text = "Bundled tools", Font = Ui.Heading, AutoSize = true };
        _toolsSummary = new Label { Text = "Checking yt-dlp and FFmpeg…", Font = Ui.Caption, AutoSize = true };
        _toolGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 3,
            Margin = new Padding(0)
        };
        _toolGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 24));
        _toolGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        _toolGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));

        foreach (var name in new[] { ToolNames.YtDlp, ToolNames.FFmpeg, ToolNames.FFprobe })
        {
            var dot = new Label { Text = "\u25CF", Font = Ui.Caption, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };
            var label = new Label { Text = name, Font = Ui.BodyStrong, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 4, 0, 4) };
            var status = new Label { Text = "Checking…", Font = Ui.Caption, AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 0, 0) };

            _toolGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            _toolGrid.Controls.Add(dot, 0, _toolRows.Count);
            _toolGrid.Controls.Add(label, 1, _toolRows.Count);
            _toolGrid.Controls.Add(status, 2, _toolRows.Count);
            _toolRows.Add((dot, label, status));
        }

        _validateTools = new AccentButton { Text = "Re-check tools", Variant = ButtonVariant.Secondary, Glyph = "\uE72C", Width = 160 };
        _validateTools.Click += async (_, _) => await ValidateToolsAsync();

        _toolsCard = BuildToolsCard();

        BuildLayout();

        _queue.ItemAdded += OnQueueChanged;
        _queue.ItemUpdated += OnQueueChanged;
        _queue.ItemRemoved += OnQueueChanged;
    }

    private CardPanel BuildActivityCard()
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 14, 0), Padding = new Padding(22) };

        var header = new Panel { Dock = DockStyle.Top, Height = 54 };
        _activityTitle.Location = new Point(0, 0);
        _activityMeta.Location = new Point(1, 28);
        header.Controls.Add(_activityMeta);
        header.Controls.Add(_activityTitle);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 44 };
        _openHistory.Location = new Point(0, 2);
        footer.Controls.Add(_openHistory);

        card.Controls.Add(_activityRows);
        card.Controls.Add(_activityEmpty);
        card.Controls.Add(footer);
        card.Controls.Add(header);
        return card;
    }

    private CardPanel BuildToolsCard()
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(22) };

        var header = new Panel { Dock = DockStyle.Top, Height = 54 };
        _toolsTitle.Location = new Point(0, 0);
        _toolsSummary.Location = new Point(1, 28);
        header.Controls.Add(_toolsSummary);
        header.Controls.Add(_toolsTitle);

        var footer = new Panel { Dock = DockStyle.Bottom, Height = 44 };
        _validateTools.Location = new Point(0, 2);
        footer.Controls.Add(_validateTools);

        card.Controls.Add(_toolGrid);
        card.Controls.Add(footer);
        card.Controls.Add(header);
        return card;
    }

    private void BuildLayout()
    {
        var stats = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 16)
        };

        for (var i = 0; i < 4; i++)
        {
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        }

        var cards = new[] { _activeCard, _pendingCard, _completedCard, _failedCard };
        for (var i = 0; i < cards.Length; i++)
        {
            cards[i].Dock = DockStyle.Fill;
            cards[i].Margin = new Padding(0, 0, i == cards.Length - 1 ? 0 : 14, 0);
            stats.Controls.Add(cards[i], i, 0);
        }

        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        body.Controls.Add(_activityCard, 0, 0);
        body.Controls.Add(_toolsCard, 1, 0);

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        shell.Controls.Add(_header, 0, 0);
        shell.Controls.Add(stats, 0, 1);
        shell.Controls.Add(body, 0, 2);

        Controls.Add(shell);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        RefreshStats();
        _ = LoadRecentAsync();
        _ = ValidateToolsAsync();
    }

    private void OnQueueChanged(object? sender, DownloadItem item)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        BeginInvoke(RefreshStats);
    }

    private void RefreshStats()
    {
        if (IsDisposed)
        {
            return;
        }

        var items = _queue.Items;

        _activeCard.ValueText = items.Count(item => item.State == DownloadState.Downloading).ToString();
        _activeCard.HintText = $"{_queue.MaxConcurrentDownloads} parallel slots";

        _pendingCard.ValueText = items.Count(item => item.State == DownloadState.Pending).ToString();
        _pendingCard.HintText = "queued links";

        _completedCard.ValueText = items.Count(item => item.State == DownloadState.Completed).ToString();
        _completedCard.HintText = "this session";

        _failedCard.ValueText = items.Count(item => item.State == DownloadState.Failed).ToString();
        _failedCard.HintText = "need attention";
        _failedCard.HintColor = items.Any(item => item.State == DownloadState.Failed) ? Palette.Danger : Color.Empty;
        _failedCard.ApplyPalette(Palette);
    }

    private async Task LoadRecentAsync()
    {
        try
        {
            var total = await _history.GetCountAsync();
            var recent = await _history.GetAsync(4);

            if (IsDisposed)
            {
                return;
            }

            _activityMeta.Text = total == 0
                ? "Nothing downloaded yet"
                : $"{total} finished download{(total == 1 ? string.Empty : "s")} in history";

            _activityRows.Controls.Clear();

            foreach (var record in recent)
            {
                _activityRows.Controls.Add(CreateActivityRow(record));
            }

            var hasRows = recent.Count > 0;
            _activityRows.Visible = hasRows;
            _activityEmpty.Visible = !hasRows;
        }
        catch (Exception)
        {
            if (!IsDisposed)
            {
                _activityMeta.Text = "History unavailable";
            }
        }
    }

    private Control CreateActivityRow(DownloadHistoryRecord record)
    {
        var row = new Panel
        {
            Width = Math.Max(200, _activityRows.ClientSize.Width - 8),
            Height = 52,
            Margin = new Padding(0, 0, 0, 6),
            BackColor = Palette.Card
        };

        var title = new Label
        {
            Text = string.IsNullOrWhiteSpace(record.Title) ? record.Url : record.Title,
            Font = Ui.BodyStrong,
            AutoSize = false,
            AutoEllipsis = true,
            Location = new Point(0, 4),
            Size = new Size(row.Width - 8, 20),
            BackColor = Palette.Card
        };

        var meta = new Label
        {
            Text = $"{record.StateText} · {record.StartedText}{(string.IsNullOrWhiteSpace(record.SizeText) ? string.Empty : " · " + record.SizeText)}",
            Font = Ui.Caption,
            AutoSize = false,
            AutoEllipsis = true,
            Location = new Point(0, 26),
            Size = new Size(row.Width - 8, 18),
            BackColor = Palette.Card
        };

        title.ForeColor = Palette.TextPrimary;
        meta.ForeColor = record.State switch
        {
            DownloadState.Completed => Palette.Success,
            DownloadState.Failed => Palette.Danger,
            _ => Palette.TextMuted
        };

        row.Controls.Add(meta);
        row.Controls.Add(title);
        return row;
    }

    private async Task ValidateToolsAsync()
    {
        _validateTools.Enabled = false;
        _toolsSummary.Text = "Checking yt-dlp and FFmpeg…";
        _toolsSummary.ForeColor = Palette.TextMuted;

        try
        {
            var report = await _tools.ValidateAsync();
            if (IsDisposed)
            {
                return;
            }

            foreach (var (dot, name, status) in _toolRows)
            {
                var tool = report.Tools.FirstOrDefault(t => t.Name == name.Text);
                var ready = tool?.IsReady == true;

                status.Text = tool is null ? "Not bundled" : ready ? "Ready" : tool.StatusText;
                status.ForeColor = ready ? Palette.Success : Palette.Warning;
                dot.ForeColor = ready ? Palette.Success : Palette.Warning;
            }

            _toolsSummary.Text = report.IsReady
                ? "All required tools are available."
                : "Some tools are missing — check Settings.";
            _toolsSummary.ForeColor = report.IsReady ? Palette.Success : Palette.Warning;
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                _toolsSummary.Text = $"Could not validate tools: {ex.Message}";
                _toolsSummary.ForeColor = Palette.Warning;
            }
        }
        finally
        {
            if (!IsDisposed)
            {
                _validateTools.Enabled = true;
            }
        }
    }

    protected override void ApplyTheme(ThemeColors palette)
    {
        _header.ApplyPalette(palette);

        foreach (var card in new[] { _activeCard, _pendingCard, _completedCard, _failedCard })
        {
            card.ApplyPalette(palette);
        }

        foreach (var card in new[] { _activityCard, _toolsCard })
        {
            card.ApplyPalette(palette);
        }

        _activityRows.BackColor = palette.Card;
        _activityEmpty.BackColor = palette.Card;
        _activityEmpty.ForeColor = palette.TextMuted;
        _activityTitle.ForeColor = palette.TextPrimary;
        _activityMeta.ForeColor = palette.TextSecondary;
        _toolsTitle.ForeColor = palette.TextPrimary;
        _toolsSummary.ForeColor = palette.TextSecondary;
        _toolGrid.BackColor = palette.Card;

        foreach (var (dot, name, status) in _toolRows)
        {
            name.ForeColor = palette.TextPrimary;
            dot.ForeColor = palette.Success;
            status.ForeColor = palette.TextSecondary;
        }

        foreach (var row in _activityRows.Controls.OfType<Panel>())
        {
            row.BackColor = palette.Card;

            foreach (var label in row.Controls.OfType<Label>())
            {
                label.BackColor = palette.Card;
            }
        }

        _openHistory.ApplyPalette(palette);
        _validateTools.ApplyPalette(palette);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _queue.ItemAdded -= OnQueueChanged;
        _queue.ItemUpdated -= OnQueueChanged;
        _queue.ItemRemoved -= OnQueueChanged;
        base.OnFormClosed(e);
    }
}
