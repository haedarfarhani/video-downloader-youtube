using Microsoft.Extensions.DependencyInjection;
using VideoDownloader.Services;
using VideoDownloader.WinForms.Controls;
using VideoDownloader.WinForms.Services;

namespace VideoDownloader.WinForms.Forms;

public partial class MainForm : Form
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localizationService;
    private readonly IUiDispatcher _uiDispatcher;
    private SidebarControl _sidebar = null!;
    private Panel _contentPanel = null!;

    private Form? _activeForm;
    private string _activeSection = string.Empty;

    public MainForm(
        IServiceProvider serviceProvider,
        IThemeService themeService,
        ILocalizationService localizationService,
        IUiDispatcher uiDispatcher)
    {
        _serviceProvider = serviceProvider;
        _themeService = themeService;
        _localizationService = localizationService;
        _uiDispatcher = uiDispatcher;

        InitializeComponent();
        themeService.ThemeChanged += OnThemeChanged;
        Navigate("Dashboard");
    }

    private void InitializeComponent()
    {
        var palette = _themeService.GetThemeColors();

        Text = "VideoDownloader";
        ClientSize = new Size(1240, 780);
        MinimumSize = new Size(1080, 700);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        BackColor = palette.Background;
        Font = Ui.Body;
        DoubleBuffered = true;

        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(32, 6, 32, 24),
            BackColor = palette.Background
        };

        _sidebar = new SidebarControl(_themeService, _localizationService, Navigate);

        // Content first, sidebar last: docking resolves from the back of the z-order,
        // so the rail claims the left edge before the content fills the remainder.
        Controls.Add(_contentPanel);
        Controls.Add(_sidebar);
    }

    private void Navigate(string section)
    {
        if (string.Equals(_activeSection, section, StringComparison.Ordinal) && _activeForm != null)
        {
            return;
        }

        if (_activeForm != null)
        {
            _activeForm.FormClosed -= OnActiveFormClosed;
            _activeForm.Close();
            _activeForm.Dispose();
            _activeForm = null;
        }

        _activeSection = section;
        var form = CreateFormForSection(section);
        if (form == null)
        {
            return;
        }

        form.TopLevel = false;
        form.FormBorderStyle = FormBorderStyle.None;
        form.Dock = DockStyle.Fill;
        form.BackColor = _themeService.GetThemeColors().Background;
        form.FormClosed += OnActiveFormClosed;

        if (form is ThemedPage page)
        {
            page.NavigateTo = Navigate;
        }

        _contentPanel.Controls.Clear();
        _contentPanel.Controls.Add(form);
        form.Show();

        _activeForm = form;
        _sidebar.SetActiveItem(section);
    }

    private Form? CreateFormForSection(string section)
    {
        return section switch
        {
            "Dashboard" => _serviceProvider.GetRequiredService<DashboardForm>(),
            "Downloads" => _serviceProvider.GetRequiredService<DownloadsForm>(),
            "Converter" => _serviceProvider.GetRequiredService<ConverterForm>(),
            "History" => _serviceProvider.GetRequiredService<HistoryForm>(),
            "Settings" => _serviceProvider.GetRequiredService<SettingsForm>(),
            _ => null
        };
    }

    private void OnActiveFormClosed(object? sender, FormClosedEventArgs e)
    {
        if (sender is Form form)
        {
            form.FormClosed -= OnActiveFormClosed;
            _activeForm = null;
        }
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (IsDisposed)
        {
            return;
        }

        var palette = _themeService.GetThemeColors();
        BackColor = palette.Background;
        _contentPanel.BackColor = palette.Background;
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        // Services are constructed before any window exists, so the dispatcher binds to the
        // main window as soon as it has a handle it can marshal calls onto.
        if (_uiDispatcher is WinFormsUiDispatcher dispatcher)
        {
            dispatcher.Attach(this);
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _themeService.ThemeChanged -= OnThemeChanged;
        base.OnFormClosed(e);
    }
}
