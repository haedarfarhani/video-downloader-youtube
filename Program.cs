using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VideoDownloader.WinForms.Forms;
using VideoDownloader.WinForms.Services;
using VideoDownloader.Services;
using VideoDownloader.Data.Repositories;
using VideoDownloader.Data;
using VideoDownloader.Configuration;
using VideoDownloader.Helpers;

namespace VideoDownloader.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

        var services = new ServiceCollection();
        services.AddLogging();
        ConfigureServices(services);
        var provider = services.BuildServiceProvider();

        var mainForm = provider.GetRequiredService<MainForm>();
        Application.Run(mainForm);

        // The download queue is a shared singleton that only implements IAsyncDisposable.
        provider.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<WinFormsUiDispatcher>();
        services.AddSingleton<IUiDispatcher>(sp => sp.GetRequiredService<WinFormsUiDispatcher>());
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        services.AddSingleton<IDownloadQueueService, DownloadQueueService>();
        services.AddTransient<IDownloadHistoryService, DownloadHistoryService>();
        services.AddTransient<IDownloadHistoryRepository, DownloadHistoryRepository>();
        services.AddSingleton<AppDatabase>();
        services.AddSingleton<AppPaths>();
        services.AddTransient<ISettingsService, SettingsService>();
        services.AddTransient<ISettingsRepository, SettingsRepository>();
        services.AddTransient<IYtDlpService, YtDlpService>();
        services.AddTransient<IFFmpegService, FFmpegService>();
        services.AddTransient<IFFprobeService, FFprobeService>();
        services.AddTransient<IMediaAnalysisService, MediaAnalysisService>();
        services.AddTransient<IToolPathService, ToolPathService>();
        services.AddTransient<IToolValidationService, ToolValidationService>();
        services.AddTransient<IDownloadPathService, DownloadPathService>();
        services.AddTransient<IFileManagementService, FileManagementService>();
        services.AddTransient<INotificationService, NotificationService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFolderPickerService, FolderPickerService>();
        services.AddSingleton<IProcessRunner, ProcessRunner>();

        services.AddTransient<MainForm>();
        services.AddTransient<DashboardForm>();
        services.AddTransient<DownloadsForm>();
        services.AddTransient<ConverterForm>();
        services.AddTransient<HistoryForm>();
        services.AddTransient<SettingsForm>();
    }
}