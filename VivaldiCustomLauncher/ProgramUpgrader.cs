#nullable enable

using System.Diagnostics;
using System.Reflection;

namespace VivaldiCustomLauncher;

internal class ProgramUpgrader(GitHubClient gitHubClient) {

    /// <summary>
    /// Install the latest version of this program from GitHub.
    /// </summary>
    /// <param name="updateNotifierPid">process ID of Vivaldi's update_notifier.exe to re-intercept after upgrading and restarting this program</param>
    /// <returns><c>true</c> if an upgrade is pending and the program should exit quickly, or <c>false</c> if no upgrade is pending and the program should resume execution</returns>
    public async Task<bool> upgrade(int? updateNotifierPid = null) {
        return await getUpgradeUri() is {} upgradeUri && await installUpgrade(upgradeUri, updateNotifierPid);
    }

    private async Task<Uri?> getUpgradeUri() {
        if (await gitHubClient.fetchLatestRelease("Aldaviva", "VivaldiCustomLauncher") is not {} latestRelease) {
            return null;
        }

        return latestRelease.version.CompareTo(Assembly.GetExecutingAssembly().GetName().Version) > 0 ? latestRelease.assetUrl : null;
    }

    private async Task<bool> installUpgrade(Uri upgradeUri, int? updateNotifierPid) {
        if (await gitHubClient.downloadRelease(upgradeUri) is not {} downloadStream) {
            return false;
        }

        string executableAbsolutePath = Assembly.GetExecutingAssembly().Location;
        string tempFile = Path.Combine(Path.GetDirectoryName(executableAbsolutePath)!,
            Path.GetFileNameWithoutExtension(executableAbsolutePath) + "-" + Cryptography.GenerateRandomString(8) + Path.GetExtension(executableAbsolutePath) + ".tmp");

        Stream fileStream;
        try {
            fileStream = new FileStream(tempFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true);
        } catch (Exception e) when (e is not OutOfMemoryException) {
            return false;
        }

        using (downloadStream)
        using (fileStream) {
            await downloadStream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
        }

        using Process selfProcess = Process.GetCurrentProcess();
        (string? selfProcessFilename, IEnumerable<string> selfProcessArgs) = Environment.GetCommandLineArgs().HeadAndTail();
        string arguments = updateNotifierPid.HasValue
            ? $"--intercept-update-notifier={updateNotifierPid.Value}"
            : selfProcessArgs.Select(static a => $"'{psEscape(a)}'").Join(", ");
        using Process? replacerProcess = Process.Start(new ProcessStartInfo {
            FileName = Environment.ExpandEnvironmentVariables(@"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"),
            Arguments = $$"""
                -NoProfile -NonInteractive -Command "& {
                    (Get-Process -Id {{selfProcess.Id}}).WaitForExit();
                    Move-Item -Force -Path '{{tempFile}}' -Destination '{{executableAbsolutePath}}';
                    Start-Process -WorkingDirectory '{{psEscape(Environment.CurrentDirectory)}}' -FilePath '{{psEscape(selfProcessFilename!)}}' -ArgumentList {{arguments}};
                }"
                """.Replace("\n", string.Empty),
            CreateNoWindow  = true,
            UseShellExecute = false
        });

        return replacerProcess is not null;
    }

    private static string psEscape(string unescaped) => unescaped.Replace("\"", "`\"");

}