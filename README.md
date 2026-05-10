# Audiobook Renamer

A Windows desktop tool for organizing audiobook libraries. Loads audio files
from a folder, fetches metadata from Goodreads, and renames/moves them to a
consistent layout.

## Features

- Load audiobooks by folder or by individual files
- Goodreads metadata lookup (title, author, series, year, cover)
- Bulk rename with preview before applying
- Locked-file preflight check (warns instead of failing mid-rename)
- ID3 tag inspection and editing

## Installation

1. Download the latest installer from the
   [Releases page](https://github.com/Pakato/AudiobookRenamer/releases/latest):
   `AudiobookRenamerSetup-X.Y.Z.exe`
2. Run the installer. SmartScreen may warn that the publisher is unverified —
   click **More info** → **Run anyway**. The app is unsigned (see notes below).
3. The installer is **per-user** and does not require admin rights. The app is
   placed in `%LOCALAPPDATA%\Programs\AudiobookRenamer` and a Start Menu
   shortcut is created.

To uninstall: Settings → Apps → Installed apps → Audiobook Renamer → Uninstall.

## Updates

The app checks for new releases on every launch. When a newer version is
available, an update dialog appears.

- Click **Update** and the app downloads the new installer, closes itself,
  installs the update silently, and restarts.
- Click **Remind Later** or **Skip** to defer.

Updates are fully automatic on subsequent launches — no manual download
needed once the initial install is in place.

If you want to update manually instead, download the latest installer from
the Releases page and run it; it will upgrade in place.

### A note on SmartScreen warnings

The installer is not code-signed yet, so Windows SmartScreen will warn on
first install and on each new version until reputation builds. Click
**More info** → **Run anyway** to proceed. Source is on GitHub, builds are
produced by the [release workflow](.github/workflows/release.yml).

## Building from source

Requirements:
- Windows 10/11 x64
- .NET 10 SDK

```powershell
git clone https://github.com/Pakato/AudiobookRenamer.git
cd AudiobookRenamer
dotnet build AudioBookManager/AudioBookManager.csproj -c Release
dotnet run --project AudioBookManager/AudioBookManager.csproj
```

To produce a local installer, install [Inno Setup 6](https://jrsoftware.org/isinfo.php)
and run:

```powershell
dotnet publish AudioBookManager/AudioBookManager.csproj -c Release -r win-x64 --self-contained true /p:Version=1.0.0 -o publish
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" /DMyAppVersion=1.0.0 /DSourceDir=$PWD\publish installer\installer.iss
```

The installer drops in `dist\`.

## Cutting a release

Tag a commit with a semver tag and push it; the GitHub Actions
[release workflow](.github/workflows/release.yml) builds the installer,
generates `update.xml`, and publishes a GitHub release with both attached.

```bash
git tag v1.0.1
git push origin v1.0.1
```

The workflow can also be triggered manually from the Actions tab with a
version input.

## License

See [LICENSE](LICENSE).
