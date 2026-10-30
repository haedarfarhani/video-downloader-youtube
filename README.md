# VideoDownloader (WinForms)

A desktop YouTube/media downloader for Windows with a modern WinForms UI on top of
`yt-dlp` and `FFmpeg`. Paste a link, queue it, watch progress, convert local media and keep a
searchable history — all in one window.

## Features

- **Downloads** — queue links for video or audio, pick a resolution, and follow live progress,
  speed and ETA for each item.
- **Converter** — inspect a local file with `ffprobe` (format, duration, resolution, streams)
  and transcode it with `FFmpeg`.
- **History** — completed, failed and cancelled downloads are persisted in a local SQLite
  database and can be reviewed or cleared.
- **Modern shell** — dark sidebar navigation, card-based pages, rounded inputs and buttons,
  hover states and empty states.
- **Light and dark themes** — the whole UI re-colors instantly, including the sidebar.
- **English and Persian interface** — switch language from the sidebar or Settings.

## Requirements

- Windows 10 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (to build) or the .NET 8 desktop
  runtime (to run a published build)
- `yt-dlp.exe`, `ffmpeg.exe` and `ffprobe.exe` — see [Tools/README.md](Tools/README.md)

## Getting started

```bash
git clone https://github.com/haedarfarhani/video-downloader-youtube.git
cd video-downloader-youtube

# 1. Drop the tools in place (they are not committed, see Tools/README.md)
#    Tools/yt-dlp/yt-dlp.exe
#    Tools/ffmpeg/ffmpeg.exe
#    Tools/ffmpeg/ffprobe.exe

# 2. Build and run
dotnet build VideoDownloader.WinForms.csproj
dotnet run --project VideoDownloader.WinForms.csproj
```

In Visual Studio, open `VideoDownloader.WinForms.csproj` and press **F5** — the tools from
`Tools/` are copied to the output folder automatically, so the same paths work while debugging.

### Publishing a distributable build

```bash
dotnet publish VideoDownloader.WinForms.csproj -c Release -r win-x64 --self-contained false -o publish
```

The published folder contains the app, its dependencies and the `Tools/` subfolder, so it can be
zipped and copied to another machine (that machine needs the .NET 8 desktop runtime).

## Where data is stored

| What | Location |
| --- | --- |
| Database and logs | `%LOCALAPPDATA%\VideoDownloader` |
| Default download folder | `%USERPROFILE%\Downloads\VideoDownloader` |
| Bundled tools | `<app folder>\Tools` (override with `VIDEO_DOWNLOADER_TOOLS_DIR`) |

## Project layout

```
VideoDownloader.WinForms.csproj   project file (build, bundled tools copy rules)
Program.cs                        composition root: DI container and app start
Forms/                            MainForm shell plus one page per section
  MainForm.cs                     sidebar + content host, navigation, dispatcher binding
  ThemedPage.cs                   base class that re-colors a page on theme changes
  DashboardForm.cs                statistics, recent activity, tool status
  DownloadsForm.cs                add-download card and live queue list
  ConverterForm.cs                file picker, ffprobe metadata, FFmpeg conversion
  HistoryForm.cs                  persisted download history
  SettingsForm.cs                 appearance, language and storage options
Controls/                         custom-drawn UI kit
  Ui.cs                           fonts, rounded geometry, card/text primitives
  CardPanel.cs, StatCard.cs       rounded containers and metric cards
  AccentButton.cs, NavItem.cs     buttons and sidebar items with hover/active states
  RoundedTextBox.cs               rounded input frame
  PageHeader.cs, SidebarControl.cs
Services/                         WinForms-specific services (theme, localization, dialogs,
                                  folder picker, UI dispatcher)
Original/                         business logic: yt-dlp/FFmpeg wrappers, queue, history,
                                  settings, SQLite persistence
Tools/                            bundled yt-dlp / FFmpeg binaries (not committed)
```

## Notes

- This repository contains the WinForms application. The `VideoDownloader.sln` file still lists
  a WinUI packaging project and a test project that live in the original, larger solution; build
  `VideoDownloader.WinForms.csproj` directly (as shown above) from a clone of this repo.
- No API keys or accounts are required — everything runs locally.
- Third-party binaries (yt-dlp, FFmpeg) keep their own licenses and are downloaded separately.

---

## فارسی

برنامهٔ دسکتاپ دانلود از یوتیوب و سایر سایتهای پشتیبانیشدهٔ `yt-dlp`، با رابط کاربری مدرن
WinForms (سایدبار تیره، کارتهای آمار، تم روشن/تاریک و رابط انگلیسی/فارسی).

**راهاندازی سریع**

1. `Tools/yt-dlp/yt-dlp.exe` و `Tools/ffmpeg/ffmpeg.exe` و `Tools/ffmpeg/ffprobe.exe` را
   قرار دهید (این فایلها در گیت کامیت نمیشوند).
2. `dotnet build VideoDownloader.WinForms.csproj`
3. `dotnet run --project VideoDownloader.WinForms.csproj`

این ابزارها هنگام اجرا از ویژوال استودیو (F5)، بیلد Release و `dotnet publish` بهطور خودکار
کنار فایل اجرایی در پوشهٔ `Tools` کپی میشوند؛ بنابراین برنامه همیشه آنها را در مسیر درست
پیدا میکند. مسیر جایگزین با متغیر محیطی `VIDEO_DOWNLOADER_TOOLS_DIR` قابل تنظیم است.
