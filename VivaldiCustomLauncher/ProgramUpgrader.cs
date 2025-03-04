#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher;

internal class ProgramUpgrader(GitHubClient gitHubClient) {

    /// <summary>
    /// Install the latest version of this program from GitHub.
    /// </summary>
    /// <returns><c>true</c> if an upgrade is pending and the program should exit quickly, or <c>false</c> if no upgrade is pending and the program should resume execution</returns>
    public async Task<bool> upgrade() {
        return await getUpgradeUri() is { } upgradeUri && await installUpgrade(upgradeUri);
    }

    private async Task<Uri?> getUpgradeUri() {
        if (await gitHubClient.fetchLatestRelease("Aldaviva", "VivaldiCustomLauncher") is not { } latestRelease) {
            return null;
        }

        return latestRelease.version.CompareTo(Assembly.GetExecutingAssembly().GetName().Version) > 0 ? latestRelease.assetUrl : null;
    }

    private async Task<bool> installUpgrade(Uri upgradeUri) {
        if (await gitHubClient.downloadRelease(upgradeUri) is not { } downloadStream) {
            return false;
        }

        string executableAbsolutePath = Assembly.GetExecutingAssembly().Location;
        string tempFile = Path.Combine(Path.GetDirectoryName(executableAbsolutePath)!,
            Path.GetFileNameWithoutExtension(executableAbsolutePath) + "-" + generateRandomString(8) + Path.GetExtension(executableAbsolutePath) + ".tmp");

        Stream fileStream;
        try {
            fileStream = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        } catch (Exception e) when (e is not OutOfMemoryException) {
            return false;
        }

        using (downloadStream)
        using (fileStream) {
            await downloadStream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
        }

        using Process? replacerProcess = Process.Start(new ProcessStartInfo {
            FileName = "cmd.exe",
            Arguments = "/q /c timeout /t 2 && " +
                $"move /y \"{tempFile}\" \"{executableAbsolutePath}\" && " +
                $"start \"\" {Environment.CommandLine}",
            CreateNoWindow  = true,
            UseShellExecute = false
        });

        return replacerProcess is not null;
    }

    /// https://stackoverflow.com/a/1344255/979493
    private static string generateRandomString(int length) {
        using RNGCryptoServiceProvider random = new();

        char[] possibleChars       = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        int    possibleCharsLength = possibleChars.Length;

        char[] result       = new char[length];
        byte[] randomBuffer = new byte[length * 4];
        random.GetBytes(randomBuffer);

        while (length-- > 0) {
            result[length] = possibleChars[BitConverter.ToUInt32(randomBuffer, length * 4) % possibleCharsLength];
        }

        return new string(result);
    }

}