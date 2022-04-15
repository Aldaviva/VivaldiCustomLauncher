using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Utf8Json;

#nullable enable

namespace VivaldiCustomLauncher;

internal class ProgramUpgrader {

    private static readonly Uri RELEASE_URI = new("https://api.github.com/repos/Aldaviva/VivaldiCustomLauncher/releases/latest");

    private readonly HttpClient            httpClient;
    private readonly VersionNumberComparer versionNumberComparer;

    public ProgramUpgrader(HttpClient httpClient, VersionNumberComparer versionNumberComparer) {
        this.httpClient            = httpClient;
        this.versionNumberComparer = versionNumberComparer;
    }

    /// <summary>
    /// Install the latest version of this program from GitHub.
    /// </summary>
    /// <returns><c>true</c> if an upgrade is pending and the program should exit quickly, or <c>false</c> if no upgrade is pending and the program should resume execution</returns>
    public async Task<bool> upgrade() {
        return await getUpgradeUri() is { } upgradeUri && await installUpgrade(upgradeUri);
    }

    private async Task<Uri?> getUpgradeUri() {
        using HttpResponseMessage metadataResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, RELEASE_URI) {
            Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json") } }
        }, HttpCompletionOption.ResponseHeadersRead);

        if (!metadataResponse.IsSuccessStatusCode) {
            return null;
        }

        using Stream metadataStream = await metadataResponse.Content.ReadAsStreamAsync();
        dynamic      metadata       = await JsonSerializer.DeserializeAsync<dynamic>(metadataStream);

        string? latestVersion = metadata["tag_name"];
        if (latestVersion is null) {
            return null;
        }

        string currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

        if (versionNumberComparer.Compare(latestVersion, currentVersion) > 0) {
            return new Uri(metadata["assets"][0]["url"]);
        } else {
            return null;
        }
    }

    private async Task<bool> installUpgrade(Uri upgradeUri) {
        using HttpResponseMessage executableResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, upgradeUri) {
            Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/octet-stream") } }
        }, HttpCompletionOption.ResponseHeadersRead);

        if (!executableResponse.IsSuccessStatusCode) {
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

        using (fileStream) {
            await executableResponse.Content.CopyToAsync(fileStream);
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