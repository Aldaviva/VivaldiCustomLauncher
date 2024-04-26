#nullable enable

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Utf8Json;

namespace VivaldiCustomLauncher;

internal class GitHubClient(HttpClient httpClient) {

    private const string BASE_URI = "https://api.github.com/";

    public async Task<(Version version, Uri assetUrl)?> fetchLatestRelease(string ownerName, string repositoryName) {
        string releaseUri = $"{BASE_URI}repos/{Uri.EscapeUriString(ownerName)}/{Uri.EscapeUriString(repositoryName)}/releases/latest";

        try {
            using HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, releaseUri) {
                Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json") } }
            }, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode) {
                return null;
            }

            using Stream metadataStream = await response.Content.ReadAsStreamAsync();
            dynamic      metadata       = await JsonSerializer.DeserializeAsync<dynamic>(metadataStream);

            return (
                version: Version.Parse(metadata["tag_name"]),
                assetUrl: new Uri(metadata["assets"][0]["url"])
            );
        } catch (HttpRequestException) {
            return null;
        }
    }

    public async Task<HttpContent?> downloadRelease(Uri assetUri) {
        try {
            HttpResponseMessage executableResponse = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, assetUri) {
                Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/octet-stream") } }
            }, HttpCompletionOption.ResponseHeadersRead);

            return executableResponse.IsSuccessStatusCode ? executableResponse.Content : null;
        } catch (HttpRequestException) {
            return null;
        }
    }

    public async Task<string?> fetchLatestCommitHash(string ownerName, string repositoryName) {
        string commitsUri = $"{BASE_URI}repos/{Uri.EscapeUriString(ownerName)}/{Uri.EscapeUriString(repositoryName)}/commits/HEAD";

        try {
            using HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, commitsUri) {
                Headers = { Accept = { new MediaTypeWithQualityHeaderValue("application/vnd.github.sha") } }
            });

            if (!response.IsSuccessStatusCode) {
                return null;
            }

            return (await response.Content.ReadAsStringAsync()).Trim();
        } catch (HttpRequestException) {
            return null;
        }
    }

}