using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Controls;

/// <summary>A dashboard metric: label, large value, hint line and a tinted glyph badge.</summary>
public sealed class StatCard : CardPanel
{
    private readonly Label _title;
    private readonly Label _value;
    private readonly Label _hint;

    public StatCard(string title, string glyph)
    {
        Glyph = glyph;
        Padding = new Padding(18, 16, 18, 14);
        ShowShadow = false;

        _title = new Label
        {
            Text = title,
            Font = Ui.Caption,
            AutoSize = true,
            Location = new Point(18, 16)
        };

        _value = new Label
        {
            Text = "0",
            Font = Ui.Metric,
            AutoSize = true,
            Location = new Point(16, 34)
        };

        _hint = new Label
        {
            Text = string.Empty,
            Font = Ui.Caption,
            AutoSize = true,
            Location = new Point(18, 74)
        };

        Controls.Add(_hint);
        Controls.Add(_value);
        Controls.Add(_title);
    }

    public string Glyph { get; }

    public string ValueText
    {
        get => _value.Text;
        set => _value.Text = value;
    }

    public string HintText
    {
        get => _hint.Text;
        set
        {
            _hint.Text = value;
            LayoutHint();
        }
    }

    public Color HintColor { get; set; } = Color.Empty;

    protected override void OnPaletteChanged()
    {
        BackColor = Palette.Background;
        _title.BackColor = Palette.Card;
        _value.BackColor = Palette.Card;
        _hint.BackColor = Palette.Card;

        _title.ForeColor = Palette.TextSecondary;
        _value.ForeColor = Palette.TextPrimary;
        _hint.ForeColor = HintColor.IsEmpty ? Palette.TextMuted : HintColor;
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        LayoutHint();
    }

    private void LayoutHint()
    {
        if (_hint is null)
        {
            return;
        }

        _hint.Location = new Point(18, Math.Max(72, Height - 34));
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var badge = new Rectangle(Width - 62, 18, 36, 36);
        Ui.Fill(e.Graphics, badge, 11, Palette.AccentSoft);
        Ui.Glyph(e.Graphics, Glyph, Ui.Icon, Palette.Accent, badge);
    }
}
