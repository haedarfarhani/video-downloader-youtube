using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using VideoDownloader.Configuration;
using VideoDownloader.Services;
using VideoDownloader.WinForms.Controls;
using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Forms;

public partial class SettingsForm : ThemedPage
{
    private readonly ILocalizationService _localization;
    private readonly AppPaths _paths;
    private readonly IToolPathService _toolPaths;

    private readonly PageHeader _header;
    private readonly AccentButton _themeButton;
    private readonly AccentButton _englishButton;
    private readonly AccentButton _persianButton;
    private readonly AccentButton _openFolderButton;

    private readonly List<CardPanel> _cards = [];
    private readonly List<Label> _titles = [];
    private readonly List<Label> _hints = [];
    private readonly List<Label> _keys = [];
    private readonly List<Label> _values = [];
    private readonly List<FlowLayoutPanel> _actionRows = [];

    public SettingsForm(IServiceProvider provider, IThemeService themeService)
        : base(themeService)
    {
        _localization = provider.GetRequiredService<ILocalizationService>();
        _paths = provider.GetRequiredService<AppPaths>();
        _toolPaths = provider.GetRequiredService<IToolPathService>();

        _header = new PageHeader("Settings", "Appearance, language and where the app keeps its files.");

        _themeButton = new AccentButton { Text = "Switch to dark", Glyph = "\uE706", Width = 158 };
        _themeButton.Click += (_, _) => ThemeService.Toggle();

        _englishButton = new AccentButton { Text = "English", Width = 106, Variant = ButtonVariant.Secondary };
        _englishButton.Click += (_, _) =>
        {
            _localization.SetLanguage("en");
            UpdateLanguageButtons();
        };

        _persianButton = new AccentButton { Text = "فارسی", Width = 106, Variant = ButtonVariant.Secondary };
        _persianButton.Click += (_, _) =>
        {
            _localization.SetLanguage("fa");
            UpdateLanguageButtons();
        };

        _openFolderButton = new AccentButton { Text = "Open folder", Variant = ButtonVariant.Secondary, Width = 140 };
        _openFolderButton.Click += (_, _) => OpenFolder(_paths.DefaultDownloadDirectory);

        BuildLayout();

        _localization.LanguageChanged += OnLanguageChanged;
        UpdateLanguageButtons();
    }

    private void BuildLayout()
    {
        var appearance = BuildRowCard(
            "Appearance",
            "Switch between the light and dark palette. Applies instantly.",
            _themeButton);

        var language = BuildRowCard(
            "Language",
            "Controls the labels used across the app.",
            _englishButton,
            _persianButton);

        var storage = BuildRowCard(
            "Download location",
            "New downloads are saved in this folder.",
            _openFolderButton);

        var locations = BuildInfoCard(
            "Storage",
            ("Version", Application.ProductVersion.Split('+')[0]),
            ("Data folder", _paths.DataDirectory),
            ("Tools folder", _toolPaths.ToolsDirectory));

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Margin = new Padding(0)
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        shell.Controls.Add(_header, 0, 0);
        shell.Controls.Add(appearance, 0, 1);
        shell.Controls.Add(language, 0, 2);
        shell.Controls.Add(storage, 0, 3);
        shell.Controls.Add(locations, 0, 4);

        Controls.Add(shell);
    }

    private CardPanel BuildRowCard(string title, string hint, params Control[] actions)
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 14), Padding = new Padding(22, 16, 22, 16) };

        var texts = new Panel { Dock = DockStyle.Fill };
        var titleLabel = new Label { Text = title, Font = Ui.Heading, AutoSize = true, Location = new Point(0, 0) };
        var hintLabel = new Label { Text = hint, Font = Ui.Caption, AutoSize = true, Location = new Point(1, 26) };
        texts.Controls.Add(hintLabel);
        texts.Controls.Add(titleLabel);

        var actionWidth = actions.Sum(action => action.Width) + (12 * Math.Max(0, actions.Length - 1));
        var actionsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = actionWidth + 8,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 2, 0, 0)
        };

        for (var i = actions.Length - 1; i >= 0; i--)
        {
            actions[i].Margin = new Padding(12, 0, 0, 0);
            actionsPanel.Controls.Add(actions[i]);
        }

        card.Controls.Add(texts);
        card.Controls.Add(actionsPanel);

        _cards.Add(card);
        _titles.Add(titleLabel);
        _hints.Add(hintLabel);
        _actionRows.Add(actionsPanel);
        return card;
    }

    private CardPanel BuildInfoCard(string title, params (string Key, string Value)[] rows)
    {
        var card = new CardPanel { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 14), Padding = new Padding(22, 16, 22, 16) };

        var titleLabel = new Label { Text = title, Font = Ui.Heading, AutoSize = true, Dock = DockStyle.Top, Height = 26 };

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = rows.Length,
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 0)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        for (var row = 0; row < rows.Length; row++)
        {
            var (key, value) = rows[row];

            var keyLabel = new Label
            {
                Text = key,
                Font = Ui.Caption,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0, 6, 0, 6)
            };

            var valueLabel = new Label
            {
                Text = value,
                Font = Ui.BodyStrong,
                AutoSize = false,
                AutoEllipsis = true,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 4, 0, 4)
            };

            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            grid.Controls.Add(keyLabel, 0, row);
            grid.Controls.Add(valueLabel, 1, row);
            _keys.Add(keyLabel);
            _values.Add(valueLabel);
        }

        card.Controls.Add(grid);
        card.Controls.Add(titleLabel);

        _cards.Add(card);
        _titles.Add(titleLabel);
        return card;
    }

    private void UpdateLanguageButtons()
    {
        var isPersian = string.Equals(_localization.CurrentLanguage, "fa", StringComparison.OrdinalIgnoreCase);
        _englishButton.Variant = isPersian ? ButtonVariant.Secondary : ButtonVariant.Primary;
        _persianButton.Variant = isPersian ? ButtonVariant.Primary : ButtonVariant.Secondary;
        _englishButton.Invalidate();
        _persianButton.Invalidate();
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => UpdateLanguageButtons();

    private void OpenFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _hints[0].Text = $"Could not open folder: {ex.Message}";
        }
    }

    protected override void ApplyTheme(ThemeColors palette)
    {
        _header.ApplyPalette(palette);

        foreach (var card in _cards)
        {
            card.ApplyPalette(palette);
        }

        foreach (var label in _titles)
        {
            label.BackColor = palette.Card;
            label.ForeColor = palette.TextPrimary;
        }

        foreach (var label in _hints)
        {
            label.BackColor = palette.Card;
            label.ForeColor = palette.TextSecondary;
        }

        foreach (var label in _keys)
        {
            label.BackColor = palette.Card;
            label.ForeColor = palette.TextSecondary;
        }

        foreach (var label in _values)
        {
            label.BackColor = palette.Card;
            label.ForeColor = palette.TextPrimary;
        }

        foreach (var row in _actionRows)
        {
            row.BackColor = palette.Card;
        }

        _themeButton.Text = palette.IsDark ? "Switch to light" : "Switch to dark";
        _themeButton.ApplyPalette(palette);
        _openFolderButton.ApplyPalette(palette);
        _englishButton.ApplyPalette(palette);
        _persianButton.ApplyPalette(palette);
        UpdateLanguageButtons();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _localization.LanguageChanged -= OnLanguageChanged;
        base.OnFormClosed(e);
    }
}
