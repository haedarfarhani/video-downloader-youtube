# Changelog

All notable changes to VideoDownloader are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html):

- `MAJOR` version for incompatible API/application changes
- `MINOR` version for added functionality in a backward-compatible manner
- `PATCH` version for backward-compatible bug fixes

Release versions are synchronized with Git tags (`v*`). Only explicit version
pushes create official releases.

---

## [Unreleased]

### Added
- CI build workflow (`.github/workflows/ci.yml`) for automated Windows builds, tests, and artifact generation.
- Release workflow (`.github/workflows/release.yml`) for automated GitHub Releases triggered by version tags (`v*`).
- Build artifact ZIP (`VideoDownloader-win-x64.zip`) with 14-day retention.
- Manual approval environment (`production`) for official releases.
- Build metadata tracking (commit SHA, version, timestamp).

### Changed
- Build now explicitly sets `PublishSingleFile=false` because the application requires external `Tools/` dependencies (`yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe`) to function correctly.
- Self-contained publishing enabled for portable Windows x64 distribution.

### Fixed
- Clear error messages for missing test projects so CI continues transparently.
- Workflow prevents silent release overwrites by verifying tag release existence before creation.

### Removed
- (None)

---

## [1.0.0] - 2026-10-30

### Added
- Initial stable release pipeline setup.
- Professional CI/CD configuration for Windows Forms .NET 8 desktop application.
- Semantic versioning support via Git tags (`v1.0.0`).
- Automated ZIP packaging (`VideoDownloader-win-x64.zip`) with runtime dependencies included.

### Changed
- Build version injected from Git tag or derived from commit SHA + run number for non-release builds.
- Artifact retention set to 14 days.

### Fixed
- Workflow uses `windows-latest` runners and detects `.NET 8 SDK` from the `.csproj` (`net8.0-windows`).
- Native library deployment handled correctly by avoiding single-file publish (incompatible with bundled external executables).

### Removed
- (None)
