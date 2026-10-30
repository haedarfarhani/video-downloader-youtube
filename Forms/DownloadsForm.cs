using Microsoft.Extensions.DependencyInjection;
using VideoDownloader.Models;
using VideoDownloader.Services;
using VideoDownloader.WinForms.Controls;
using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Forms;

public partial class DownloadsForm : ThemedPage
{
    private readonly IDownloadQueueService _queue;
    private readonly IToolValidationService _tools;

    private readonly PageHeader _header;
    private readonly CardPanel _addCard;
    private readonly Label _addTitle;
    private readonly Label _addHint;
    private readonly RoundedTextBox _urlBox;
    private readonly ComboBox _modeBox;
    private readonly ComboBox _qualityBox;
    private readonly AccentButton _addButton;

    private readonly CardPanel _queueCard;
    private readonly Label _queueTitle;
    private readonly Label _queueSummary;
    private readonly AccentButton _clearButton;
    private readonly ListBox _list;
    private readonly Label _empty;

    private List<DownloadItem> _rows = [];

    public DownloadsForm(IServiceProvider provider, IThemeService themeService)
        : base(themeService)
    {
        _queue = provider.GetRequiredService<IDownloadQueueService>();
        _tools = provider.GetRequiredService<IToolValidationService>();

        _header = new PageHeader("Downloads", "Queue links from YouTube and other yt-dlp supported sites.");

        _addTitle = new Label { Text = "New download", Font = Ui.Heading, AutoSize = true };
        _addHint = new Label
        {
            Text = "Paste a link, choose a format and add it to the queue.",
            Font = Ui.Caption,
            AutoSize = true
        };

        _urlBox = new RoundedTextBox("https://www.youtube.com/watch?v=…") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };

        _qualityBox = CreateCombo("Best available", "2160p", "1440p", "1080p", "720p", "480p");
        _qualityBox.Width = 140;

        _modeBox = CreateCombo("Video", "Audio");
        _modeBox.Width = 118;
        _modeBox.SelectedIndexChanged += (_, _) => _qualityBox.Enabled = _modeBox.SelectedIndex == 0;

        _addButton = new AccentButton { Text = "Add to queue", Glyph = "\uE710", Width = 168 };
        _addButton.Click += OnAddClicked;

        _addCard = BuildAddCard();

        _queueTitle = new Label { Text = "Download queue", Font = Ui.Heading, AutoSize = true };
        _queueSummary = new Label { Text = "Nothing queued yet", Font = Ui.Caption, AutoSize = true };

        _clearButton = new AccentButton { Text = "Clear finished", Variant = ButtonVariant.Secondary, Width = 148 };
        _clearButton.Click += (_, _) =>
        {
            _queue.ClearFinished();
            RefreshQueue();
        };

        _empty = new Label
        {
            Text = "No downloads yet.\nAdd a link above to get started.",
            Font = Ui.Body,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };

        _list = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 78,
            IntegralHeight = false,
            SelectionMode = SelectionMode.One,
            Font = Ui.Body
        };
        _list.DrawItem += DrawQueueItem;

        _queueCard = BuildQueueCard();

        BuildLayout();

        _queue.ItemAdded += OnQueueChanged;
        _queue.ItemUpdated += OnQueueChanged;
        _queue.ItemRemoved += OnQueueChanged;
    }

    private static ComboBox CreateCombo(params string[] options)
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Font = Ui.Body,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 12, 0)
        };

        combo.Items.AddRange(options);
        combo.SelectedIndex = 0;
        return combo;
    }

    private CardPanel BuildAddCard()
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(22, 18, 22, 18) };

        var header = new Panel { Dock = DockStyle.Top, Height = 54 };
        _addTitle.Location = new Point(0, 0);
        _addHint.Location = new Point(1, 28);
        header.Controls.Add(_addHint);
        header.Controls.Add(_addTitle);

        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            ColumnCount = 4,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 152));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 168));

        _urlBox.Height = 42;
        row.Controls.Add(_urlBox, 0, 0);
        row.Controls.Add(_modeBox, 1, 0);
        row.Controls.Add(_qualityBox, 2, 0);
        row.Controls.Add(_addButton, 3, 0);

        card.Controls.Add(row);
        card.Controls.Add(header);
        return card;
    }

    private CardPanel BuildQueueCard()
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 16, 0, 0), Padding = new Padding(22, 18, 22, 18) };

        var texts = new Panel { Dock = DockStyle.Fill };
        _queueTitle.Location = new Point(0, 0);
        _queueSummary.Location = new Point(1, 26);
        texts.Controls.Add(_queueSummary);
        texts.Controls.Add(_queueTitle);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 52,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 148));
        header.Controls.Add(texts, 0, 0);
        header.Controls.Add(_clearButton, 1, 0);

        card.Controls.Add(_list);
        card.Controls.Add(_empty);
        card.Controls.Add(header);
        return card;
    }

    private void BuildLayout()
    {
        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0)
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 148));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        shell.Controls.Add(_header, 0, 0);
        shell.Controls.Add(_addCard, 0, 1);
        shell.Controls.Add(_queueCard, 0, 2);

        Controls.Add(shell);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        RefreshQueue();
    }

    private void OnQueueChanged(object? sender, DownloadItem item)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        BeginInvoke(RefreshQueue);
    }

    private void RefreshQueue()
    {
        if (IsDisposed)
        {
            return;
        }

        _rows = [.. _queue.Items];

        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var item in _rows)
        {
            _list.Items.Add(item);
        }

        _list.EndUpdate();

        var active = _rows.Count(item => item.State == DownloadState.Downloading);
        _queueSummary.Text = _rows.Count == 0
            ? "Nothing queued yet"
            : $"{_rows.Count} item{(_rows.Count == 1 ? string.Empty : "s")} · {active} downloading · {_queue.MaxConcurrentDownloads} parallel";

        var hasRows = _rows.Count > 0;
        _list.Visible = hasRows;
        _empty.Visible = !hasRows;
        _clearButton.Enabled = _rows.Any(item => item.State is DownloadState.Completed or DownloadState.Failed or DownloadState.Cancelled);
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        var url = _urlBox.Text.Trim();

        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
            (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            SetHint("Enter a valid http(s) link.", Palette.Warning);
            return;
        }

        _addButton.Enabled = false;
        SetHint("Checking tools…", Palette.TextMuted);

        try
        {
            var report = await _tools.EnsureReadyAsync();
            if (!report.IsReady)
            {
                SetHint(report.Summary, Palette.Warning);
                return;
            }

            var options = new DownloadOptions
            {
                Url = url,
                Mode = _modeBox.SelectedIndex == 1 ? DownloadMode.Audio : DownloadMode.Video,
                MaxVideoHeight = SelectedQualityHeight(),
                Title = url
            };

            var item = _queue.Enqueue(options);
            _urlBox.Text = string.Empty;
            SetHint($"Queued “{item.Title}”.", Palette.Success);
            RefreshQueue();
        }
        catch (Exception ex)
        {
            SetHint(ex.Message, Palette.Danger);
        }
        finally
        {
            if (!IsDisposed)
            {
                _addButton.Enabled = true;
            }
        }
    }

    private int SelectedQualityHeight() => _qualityBox.SelectedIndex switch
    {
        1 => 2160,
        2 => 1440,
        3 => 1080,
        4 => 720,
        5 => 480,
        _ => 0
    };

    private void SetHint(string message, Color color)
    {
        _addHint.Text = message;
        _addHint.ForeColor = color;
    }

    private void DrawQueueItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _rows.Count)
        {
            return;
        }

        var palette = Palette;
        var item = _rows[e.Index];
        var bounds = e.Bounds;

        var rowColor = (e.State & DrawItemState.Selected) == DrawItemState.Selected
            ? palette.CardHover
            : palette.Card;

        using (var rowBrush = new SolidBrush(rowColor))
        {
            e.Graphics.FillRectangle(rowBrush, bounds);
        }

        Ui.Prepare(e.Graphics);

        var chipColor = item.State switch
        {
            DownloadState.Completed => palette.SuccessSoft,
            DownloadState.Failed => palette.DangerSoft,
            DownloadState.Downloading => palette.AccentSoft,
            _ => palette.Chip
        };

        var chipTextColor = item.State switch
        {
            DownloadState.Completed => palette.Success,
            DownloadState.Failed => palette.Danger,
            DownloadState.Downloading => palette.Accent,
            _ => palette.TextSecondary
        };

        Ui.SingleLine(e.Graphics, string.IsNullOrWhiteSpace(item.Title) ? item.Url : item.Title, Ui.BodyStrong, palette.TextPrimary,
            new Rectangle(bounds.X + 14, bounds.Y + 10, bounds.Width - 150, 22), Ui.Left);

        var chip = new Rectangle(bounds.Right - 116, bounds.Y + 11, 96, 24);
        Ui.Fill(e.Graphics, chip, 12, chipColor);
        Ui.Text(e.Graphics, item.StateText, Ui.CaptionStrong, chipTextColor, chip, Ui.Center);

        var track = new Rectangle(bounds.X + 14, bounds.Y + 42, bounds.Width - 160, 6);
        Ui.Fill(e.Graphics, track, 3, palette.Chip);

        var percent = Math.Clamp(item.ProgressPercent, 0, 100);
        if (percent > 0)
        {
            var width = (int)Math.Max(6, track.Width * percent / 100.0);
            var fill = new Rectangle(track.X, track.Y, Math.Min(width, track.Width), track.Height);
            var progressColor = item.State switch
            {
                DownloadState.Completed => palette.Success,
                DownloadState.Failed => palette.Danger,
                _ => palette.Accent
            };
            Ui.Fill(e.Graphics, fill, 3, progressColor);
        }

        var meta = new List<string> { item.ProgressText };
        if (!string.IsNullOrWhiteSpace(item.SizeText))
        {
            meta.Add(item.SizeText);
        }

        if (!string.IsNullOrWhiteSpace(item.SpeedText))
        {
            meta.Add(item.SpeedText);
        }

        if (!string.IsNullOrWhiteSpace(item.EtaText))
        {
            meta.Add(item.EtaText);
        }

        if (item.ShowError && !string.IsNullOrWhiteSpace(item.ErrorMessageText))
        {
            meta.Add(item.ErrorMessageText);
        }

        Ui.SingleLine(e.Graphics, string.Join("  ·  ", meta), Ui.Caption,
            item.State == DownloadState.Failed ? palette.Danger : palette.TextSecondary,
            new Rectangle(bounds.X + 14, bounds.Y + 52, bounds.Width - 34, 18), Ui.Left);

        using var divider = new Pen(palette.Divider);
        e.Graphics.DrawLine(divider, bounds.X + 14, bounds.Bottom - 1, bounds.Right - 14, bounds.Bottom - 1);
    }

    protected override void ApplyTheme(ThemeColors palette)
    {
        _header.ApplyPalette(palette);
        _addCard.ApplyPalette(palette);
        _queueCard.ApplyPalette(palette);

        _addTitle.ForeColor = palette.TextPrimary;
        _addHint.ForeColor = palette.TextSecondary;
        _queueTitle.ForeColor = palette.TextPrimary;
        _queueSummary.ForeColor = palette.TextSecondary;

        _urlBox.ApplyPalette(palette);
        _addButton.ApplyPalette(palette);
        _clearButton.ApplyPalette(palette);

        foreach (var combo in new[] { _modeBox, _qualityBox })
        {
            combo.BackColor = palette.Input;
            combo.ForeColor = palette.TextPrimary;
        }

        _list.BackColor = palette.Card;
        _list.ForeColor = palette.TextPrimary;
        _empty.BackColor = palette.Card;
        _empty.ForeColor = palette.TextMuted;
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _queue.ItemAdded -= OnQueueChanged;
        _queue.ItemUpdated -= OnQueueChanged;
        _queue.ItemRemoved -= OnQueueChanged;
        base.OnFormClosed(e);
    }
}
