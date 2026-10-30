using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Controls;

/// <summary>Title block shown at the top of every page.</summary>
public sealed class PageHeader : Panel
{
    private readonly Label _title;
    private readonly Label _subtitle;

    public PageHeader(string title, string subtitle)
    {
        Dock = DockStyle.Top;
        Height = 86;

        _title = new Label
        {
            Text = title,
            Font = Ui.Display,
            AutoSize = true,
            Location = new Point(0, 2)
        };

        _subtitle = new Label
        {
            Text = subtitle,
            Font = Ui.Caption,
            AutoSize = true,
            Location = new Point(2, 46)
        };

        Controls.Add(_subtitle);
        Controls.Add(_title);
    }

    public string Title
    {
        get => _title.Text;
        set => _title.Text = value;
    }

    public string Subtitle
    {
        get => _subtitle.Text;
        set => _subtitle.Text = value;
    }

    public void ApplyPalette(ThemeColors palette)
    {
        BackColor = palette.Background;
        _title.ForeColor = palette.TextPrimary;
        _subtitle.ForeColor = palette.TextSecondary;
    }
}
