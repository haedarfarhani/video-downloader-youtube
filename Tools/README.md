# Bundled tools

The application shells out to `yt-dlp` and `FFmpeg`, and expects them in this layout:

```
Tools/
├── yt-dlp/
│   └── yt-dlp.exe
└── ffmpeg/
    ├── ffmpeg.exe
    └── ffprobe.exe
```

Anything placed in this folder is copied next to the executable for **debug runs, Release
builds and publish output**, so the app always resolves the tools from
`AppContext.BaseDirectory\Tools`. A different location can be forced at runtime with the
`VIDEO_DOWNLOADER_TOOLS_DIR` environment variable.

The executables are **not committed** to git (see `.gitignore`), because they are large
third-party binaries. Download them once and drop them in the folders above:

- yt-dlp: <https://github.com/yt-dlp/yt-dlp/releases/latest> → `yt-dlp.exe`
- FFmpeg (includes `ffprobe`): <https://www.gyan.dev/ffmpeg/builds/> → `ffmpeg-release-essentials.zip`
  (use the `bin/ffmpeg.exe` and `bin/ffprobe.exe` from the archive)

Without these files the UI still starts, but downloads and conversions will report that the
tools are missing and the build emits a warning naming this directory.
