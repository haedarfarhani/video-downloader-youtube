using Microsoft.Extensions.DependencyInjection;
using VideoDownloader.Helpers;
using VideoDownloader.Services;
using VideoDownloader.WinForms.Controls;
using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Forms;

public partial class ConverterForm : ThemedPage
{
    private readonly IFFmpegService _ffmpeg;
    private readonly IFFprobeService _ffprobe;

    private readonly PageHeader _header;
    private readonly CardPanel _sourceCard;
    private readonly Label _sourceTitle;
    private readonly Label _sourceHint;
    private readonly RoundedTextBox _pathBox;
    private readonly AccentButton _browseButton;
    private readonly AccentButton _inspectButton;
    private readonly AccentButton _convertButton;

    private readonly CardPanel _resultCard;
    private readonly Label _resultTitle;
    private readonly Label _resultHint;
    private readonly TableLayoutPanel _resultGrid;

    private readonly List<(Label Key, Label Value)> _resultRows = [];

    public ConverterForm(IServiceProvider provider, IThemeService themeService)
        : base(themeService)
    {
        _ffmpeg = provider.GetRequiredService<IFFmpegService>();
        _ffprobe = provider.GetRequiredService<IFFprobeService>();

        _header = new PageHeader("Converter", "Inspect a media file and transcode it with the bundled FFmpeg.");

        _sourceTitle = new Label { Text = "Source file", Font = Ui.Heading, AutoSize = true };
        _sourceHint = new Label
        {
            Text = "Choose a local video or audio file to analyse.",
            Font = Ui.Caption,
            AutoSize = true
        };

        _pathBox = new RoundedTextBox(@"C:\path\to\video.mp4") { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 12, 0) };

        _browseButton = new AccentButton { Text = "Browse", Variant = ButtonVariant.Secondary, Width = 128 };
        _browseButton.Click += (_, _) => BrowseForFile();

        _inspectButton = new AccentButton { Text = "Inspect", Variant = ButtonVariant.Secondary, Glyph = "\uE721", Width = 150 };
        _inspectButton.Click += async (_, _) => await InspectAsync();

        _convertButton = new AccentButton { Text = "Convert to MP4", Glyph = "\uE895", Width = 176 };
        _convertButton.Click += async (_, _) => await ConvertAsync();

        _sourceCard = BuildSourceCard();

        _resultTitle = new Label { Text = "Media details", Font = Ui.Heading, AutoSize = true };
        _resultHint = new Label { Text = "Nothing inspected yet.", Font = Ui.Caption, AutoSize = true };
        _resultGrid = BuildResultGrid();
        _resultCard = BuildResultCard();

        BuildLayout();
    }

    private CardPanel BuildSourceCard()
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(22, 18, 22, 18) };

        var header = new Panel { Dock = DockStyle.Top, Height = 54 };
        _sourceTitle.Location = new Point(0, 0);
        _sourceHint.Location = new Point(1, 28);
        header.Controls.Add(_sourceHint);
        header.Controls.Add(_sourceTitle);

        var pathRow = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 46,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 128));
        _pathBox.Height = 42;
        pathRow.Controls.Add(_pathBox, 0, 0);
        pathRow.Controls.Add(_browseButton, 1, 0);

        var actionRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0),
            Margin = new Padding(0)
        };
        _inspectButton.Margin = new Padding(0, 0, 12, 0);
        actionRow.Controls.Add(_inspectButton);
        actionRow.Controls.Add(_convertButton);

        card.Controls.Add(actionRow);
        card.Controls.Add(pathRow);
        card.Controls.Add(header);
        return card;
    }

    private TableLayoutPanel BuildResultGrid()
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = true,
            Margin = new Padding(0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        foreach (var key in new[] { "Format", "Duration", "Resolution", "Size", "Streams" })
        {
            var keyLabel = new Label
            {
                Text = key,
                Font = Ui.Caption,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 7, 0, 7)
            };

            var valueLabel = new Label
            {
                Text = "—",
                Font = Ui.BodyStrong,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 5, 0, 5)
            };

            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            grid.Controls.Add(keyLabel, 0, _resultRows.Count);
            grid.Controls.Add(valueLabel, 1, _resultRows.Count);
            _resultRows.Add((keyLabel, valueLabel));
        }

        return grid;
    }

    private CardPanel BuildResultCard()
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 16, 0, 0), Padding = new Padding(22, 18, 22, 18) };

        var header = new Panel { Dock = DockStyle.Top, Height = 54 };
        _resultTitle.Location = new Point(0, 0);
        _resultHint.Location = new Point(1, 28);
        header.Controls.Add(_resultHint);
        header.Controls.Add(_resultTitle);

        card.Controls.Add(_resultGrid);
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
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 204));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        shell.Controls.Add(_header, 0, 0);
        shell.Controls.Add(_sourceCard, 0, 1);
        shell.Controls.Add(_resultCard, 0, 2);

        Controls.Add(shell);
    }

    private void BrowseForFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select a media file",
            Filter = "Media files|*.mp4;*.mkv;*.webm;*.mov;*.avi;*.mp3;*.m4a;*.wav;*.flac;*.opus|All files|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _pathBox.Text = dialog.FileName;
        }
    }

    private async Task InspectAsync()
    {
        var path = _pathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            SetHint("Pick a file first.", Palette.Warning);
            return;
        }

        _inspectButton.Enabled = false;
        SetHint("Inspecting…", Palette.TextMuted);

        try
        {
            var metadata = await _ffprobe.InspectAsync(path);
            if (IsDisposed)
            {
                return;
            }

            if (metadata is null)
            {
                SetHint("No metadata returned for this file.", Palette.Warning);
                return;
            }

            var video = metadata.Streams.FirstOrDefault(stream => stream.CodecType == "video");
            var resolution = video is { Width: not null, Height: not null }
                ? $"{video.Width}×{video.Height}"
                : "—";

            SetRow(0, string.IsNullOrWhiteSpace(metadata.FormatLongName) ? metadata.FormatName : metadata.FormatLongName);
            SetRow(1, TimeSpan.FromSeconds(metadata.DurationSeconds).ToString(@"hh\:mm\:ss"));
            SetRow(2, resolution);
            SetRow(3, metadata.SizeBytes > 0 ? ByteFormatter.Format(metadata.SizeBytes) : "—");
            SetRow(4, $"{metadata.Streams.Count(s => s.CodecType == "video")} video · {metadata.Streams.Count(s => s.CodecType == "audio")} audio");
            SetHint("Inspection finished.", Palette.Success);
        }
        catch (Exception ex)
        {
            SetHint(ex.Message, Palette.Danger);
        }
        finally
        {
            if (!IsDisposed)
            {
                _inspectButton.Enabled = true;
            }
        }
    }

    private async Task ConvertAsync()
    {
        var path = _pathBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(path))
        {
            SetHint("Pick a file first.", Palette.Warning);
            return;
        }

        _convertButton.Enabled = false;
        SetHint("Converting… this can take a while.", Palette.TextMuted);

        try
        {
            var output = Path.ChangeExtension(path, ".converted.mp4");
            var result = await _ffmpeg.ConvertAsync(path, output);
            if (IsDisposed)
            {
                return;
            }

            SetHint(result.Success
                    ? $"Converted to {result.OutputFilePath ?? output}"
                    : result.ErrorMessage ?? "Conversion failed.",
                result.Success ? Palette.Success : Palette.Danger);
        }
        catch (Exception ex)
        {
            SetHint(ex.Message, Palette.Danger);
        }
        finally
        {
            if (!IsDisposed)
            {
                _convertButton.Enabled = true;
            }
        }
    }

    private void SetRow(int index, string value)
    {
        if (index >= 0 && index < _resultRows.Count)
        {
            _resultRows[index].Value.Text = value;
        }
    }

    private void SetHint(string message, Color color)
    {
        _resultHint.Text = message;
        _resultHint.ForeColor = color;
    }

    protected override void ApplyTheme(ThemeColors palette)
    {
        _header.ApplyPalette(palette);
        _sourceCard.ApplyPalette(palette);
        _resultCard.ApplyPalette(palette);

        _sourceTitle.ForeColor = palette.TextPrimary;
        _sourceHint.ForeColor = palette.TextSecondary;
        _resultTitle.ForeColor = palette.TextPrimary;
        _resultHint.ForeColor = palette.TextSecondary;

        _pathBox.ApplyPalette(palette);
        _browseButton.ApplyPalette(palette);
        _inspectButton.ApplyPalette(palette);
        _convertButton.ApplyPalette(palette);

        _resultGrid.BackColor = palette.Card;

        foreach (var (key, value) in _resultRows)
        {
            key.BackColor = palette.Card;
            value.BackColor = palette.Card;
            key.ForeColor = palette.TextSecondary;
            value.ForeColor = palette.TextPrimary;
        }
    }
}
