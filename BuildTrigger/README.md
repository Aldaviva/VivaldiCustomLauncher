VivaldiCustomLauncherBuildTrigger
===

Checks the latest [Vivaldi](https://vivaldi.com/desktop/) release and snapshot versions, and if either one is newer than the tested versions in [VivaldiCustomLauncher](https://github.com/Aldaviva/VivaldiCustomLauncher), then trigger the [build](https://github.com/Aldaviva/VivaldiCustomLauncher/actions/workflows/build.yml) that automatically downloads and tests against the latest version.

## Prerequisites
- [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) or later

## Usage
```ps1
VivaldiCustomLauncherBuildTrigger.exe --github-access-token ghp_XXXXXXXXXXXX
```
This can be run in a scheduled task or cron job.

### Arguments
#### `--github-access-token`
A [GitHub personal access token](https://github.com/settings/tokens) with the `repo` scope.

#### `--dry-run`
Don't trigger the build, even if a newer version is found.