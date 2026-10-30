using Microsoft.Extensions.DependencyInjection;
using VideoDownloader.Models;
using VideoDownloader.Services;
using VideoDownloader.WinForms.Controls;
using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Forms;

public partial class HistoryForm : ThemedPage
{
    private readonly IDownloadHistoryService _history;
    private readonly IDialogService _dialogs;

    private readonly PageHeader _header;
    private readonly CardPanel _card;
    private readonly Label _title;
    private readonly Label _summary;
    private readonly AccentButton _refreshButton;
    private readonly AccentButton _clearButton;
    private readonly ListBox _list;
    private readonly Label _empty;

    private List<DownloadHistoryRecord> _rows = [];

    public HistoryForm(IServiceProvider provider, IThemeService themeService)
        : base(themeService)
    {
        _history = provider.GetRequiredService<IDownloadHistoryService>();
        _dialogs = provider.GetRequiredService<IDialogService>();

        _header = new PageHeader("History", "Every download that finished, failed or was cancelled.");

        _title = new Label { Text = "Download history", Font = Ui.Heading, AutoSize = true };
        _summary = new Label { Text = "Loading…", Font = Ui.Caption, AutoSize = true };

        _refreshButton = new AccentButton { Text = "Refresh", Variant = ButtonVariant.Secondary, Glyph = "\uE72C", Width = 122 };
        _refreshButton.Click += async (_, _) => await LoadAsync();

        _clearButton = new AccentButton { Text = "Clear all", Variant = ButtonVariant.Danger, Width = 118 };
        _clearButton.Click += async (_, _) => await ClearAsync();

        _empty = new Label
        {
            Text = "Nothing here yet.\nFinished downloads are recorded automatically.",
            Font = Ui.Body,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill
        };

        _list = new ListBox
        {
            Dock = DockStyle.Fill,
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 66,
            IntegralHeight = false,
            SelectionMode = SelectionMode.One,
            Font = Ui.Body
        };
        _list.DrawItem += DrawHistoryItem;

        _card = BuildCard();
        BuildLayout();
    }

    private CardPanel BuildCard()
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(22, 18, 22, 18) };

        var texts = new Panel { Dock = DockStyle.Fill };
        _title.Location = new Point(0, 0);
        _summary.Location = new Point(1, 26);
        texts.Controls.Add(_summary);
        texts.Controls.Add(_title);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 256,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 2, 0, 0)
        };
        _clearButton.Margin = new Padding(10, 0, 0, 0);
        actions.Controls.Add(_clearButton);
        actions.Controls.Add(_refreshButton);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 54,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 256));
        header.Controls.Add(texts, 0, 0);
        header.Controls.Add(actions, 1, 0);

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
            RowCount = 2,
            Margin = new Padding(0)
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        shell.Controls.Add(_header, 0, 0);
        shell.Controls.Add(_card, 0, 1);

        Controls.Add(shell);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        _refreshButton.Enabled = false;
        _summary.Text = "Loading…";

        try
        {
            var records = await _history.GetAsync(300);
            if (IsDisposed)
            {
                return;
            }

            _rows = [.. records];

            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var record in _rows)
            {
                _list.Items.Add(record);
            }

            _list.EndUpdate();

            var hasRows = _rows.Count > 0;
            _list.Visible = hasRows;
            _empty.Visible = !hasRows;
            _clearButton.Enabled = hasRows;

            if (!hasRows)
            {
                _summary.Text = "Nothing saved yet";
                return;
            }

            var completed = _rows.Count(record => record.State == DownloadState.Completed);
            var failed = _rows.Count(record => record.State == DownloadState.Failed);
            _summary.Text = $"{_rows.Count} record{(_rows.Count == 1 ? string.Empty : "s")} · {completed} completed · {failed} failed";
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                _summary.Text = $"Could not load history: {ex.Message}";
            }
        }
        finally
        {
            if (!IsDisposed)
            {
                _refreshButton.Enabled = true;
            }
        }
    }

    private async Task ClearAsync()
    {
        if (_rows.Count == 0)
        {
            return;
        }

        var confirmed = await _dialogs.ConfirmAsync("Clear history", "Delete every saved history record? This cannot be undone.");
        if (!confirmed)
        {
            return;
        }

        try
        {
            await _history.ClearAsync();
            await LoadAsync();
        }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                _summary.Text = $"Could not clear history: {ex.Message}";
            }
        }
    }

    private void DrawHistoryItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _rows.Count)
        {
            return;
        }

        var palette = Palette;
        var record = _rows[e.Index];
        var bounds = e.Bounds;

        using (var background = new SolidBrush((e.State & DrawItemState.Selected) == DrawItemState.Selected ? palette.CardHover : palette.Card))
        {
            e.Graphics.FillRectangle(background, bounds);
        }

        Ui.Prepare(e.Graphics);

        var chipColor = record.State switch
        {
            DownloadState.Completed => palette.SuccessSoft,
            DownloadState.Failed => palette.DangerSoft,
            _ => palette.Chip
        };

        var chipTextColor = record.State switch
        {
            DownloadState.Completed => palette.Success,
            DownloadState.Failed => palette.Danger,
            _ => palette.TextSecondary
        };

        Ui.SingleLine(e.Graphics, string.IsNullOrWhiteSpace(record.Title) ? record.Url : record.Title, Ui.BodyStrong,
            palette.TextPrimary, new Rectangle(bounds.X + 14, bounds.Y + 10, bounds.Width - 160, 22), Ui.Left);

        var meta = new List<string> { record.ModeText, record.StartedText };
        if (!string.IsNullOrWhiteSpace(record.SizeText))
        {
            meta.Add(record.SizeText);
        }

        if (!string.IsNullOrWhiteSpace(record.OutputPath))
        {
            meta.Add(Path.GetFileName(record.OutputPath));
        }

        Ui.SingleLine(e.Graphics, string.Join("  ·  ", meta), Ui.Caption, palette.TextSecondary,
            new Rectangle(bounds.X + 14, bounds.Y + 34, bounds.Width - 160, 18), Ui.Left);

        var chip = new Rectangle(bounds.Right - 120, bounds.Y + 20, 100, 24);
        Ui.Fill(e.Graphics, chip, 12, chipColor);
        Ui.Text(e.Graphics, record.StateText, Ui.CaptionStrong, chipTextColor, chip, Ui.Center);

        using var divider = new Pen(palette.Divider);
        e.Graphics.DrawLine(divider, bounds.X + 14, bounds.Bottom - 1, bounds.Right - 14, bounds.Bottom - 1);
    }

    protected override void ApplyTheme(ThemeColors palette)
    {
        _header.ApplyPalette(palette);
        _card.ApplyPalette(palette);

        _title.ForeColor = palette.TextPrimary;
        _summary.ForeColor = palette.TextSecondary;

        _refreshButton.ApplyPalette(palette);
        _clearButton.ApplyPalette(palette);

        _list.BackColor = palette.Card;
        _list.ForeColor = palette.TextPrimary;
        _empty.BackColor = palette.Card;
        _empty.ForeColor = palette.TextMuted;
    }
}
