using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Controls;

/// <summary>A rounded input frame that hosts a borderless <see cref="TextBox"/>.</summary>
public sealed class RoundedTextBox : Panel
{
    private readonly TextBox _box;
    private ThemeColors _palette = ThemeColors.Light;
    private bool _focused;

    public RoundedTextBox(string placeholder = "")
    {
        SetStyle(
            ControlStyles.UserPaint |
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw, true);

        Height = 42;
        Padding = new Padding(14, 11, 12, 9);

        _box = new TextBox
        {
            BorderStyle = BorderStyle.None,
            Font = Ui.Body,
            Dock = DockStyle.Fill,
            PlaceholderText = placeholder
        };

        _box.GotFocus += (_, _) =>
        {
            _focused = true;
            Invalidate();
        };

        _box.LostFocus += (_, _) =>
        {
            _focused = false;
            Invalidate();
        };

        Controls.Add(_box);
    }

    public TextBox InnerTextBox => _box;

    [System.Diagnostics.CodeAnalysis.AllowNull]
    public override string Text
    {
        get => _box.Text;
        set => _box.Text = value ?? string.Empty;
    }

    public string PlaceholderText
    {
        get => _box.PlaceholderText;
        set => _box.PlaceholderText = value;
    }

    public event EventHandler? ValueChanged
    {
        add => _box.TextChanged += value;
        remove => _box.TextChanged -= value;
    }

    public void ApplyPalette(ThemeColors palette)
    {
        _palette = palette;
        BackColor = palette.Card;
        _box.BackColor = palette.Input;
        _box.ForeColor = palette.TextPrimary;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
        Ui.Prepare(e.Graphics);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        Ui.Fill(e.Graphics, bounds, 9, _palette.Input);
        Ui.Stroke(e.Graphics, bounds, 9, _focused ? _palette.Accent : _palette.InputBorder, _focused ? 1.6f : 1f);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
    }
}
