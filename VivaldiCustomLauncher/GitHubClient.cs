#nullable enable

using System;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Unfucked;
using Unfucked.HTTP;
using Unfucked.HTTP.Exceptions;

namespace VivaldiCustomLauncher;

internal class GitHubClient(HttpClient httpClient) {

    private static readonly Uri        BASE_URI      = new("https://api.github.com/");
    private static readonly UrlBuilder REPO_TEMPLATE = new UrlBuilder(BASE_URI).Path("repos/{owner}/{repo}");

    public async Task<(Version version, Uri assetUrl)?> fetchLatestRelease(string ownerName, string repositoryName) {
        try {
            using JsonDocument metadata = await httpClient.Target(REPO_TEMPLATE)
                .Path("releases", "latest")
                .ResolveTemplate("owner", ownerName)
                .ResolveTemplate("repo", repositoryName)
                .Accept("application/vnd.github.v3+json")
                .Get<JsonDocument>();

            return (
                version: Version.Parse(metadata.RootElement.GetProperty("tag_name").GetString()!),
                assetUrl: new Uri(metadata.RootElement.GetProperty("assets")[0].GetProperty("url").GetString()!)
            );
        } catch (HttpException) {
            return null;
        }
    }

    public async Task<Stream?> downloadRelease(Uri assetUri) {
        try {
            return await httpClient.Target(assetUri)
                .Accept(MediaTypeNames.Application.Octet)
                .Get<Stream>();
        } catch (HttpException) {
            return null;
        }
    }

    public async Task<string?> fetchLatestCommitHash(string ownerName, string repositoryName) {
        try {
            return await httpClient.Target(REPO_TEMPLATE)
                .Path("commits", "HEAD")
                .ResolveTemplate("owner", ownerName)
                .ResolveTemplate("repo", repositoryName)
                .Accept("application/vnd.github.sha")
                .Get<string>();
        } catch (HttpException) {
            return null;
        }
    }

}