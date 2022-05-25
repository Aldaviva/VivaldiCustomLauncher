#nullable enable

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks;

public abstract class BaseDownloadableTweak: BaseStreamTweak<BaseTweakParams> {

    private readonly HttpClient httpClient;

    protected abstract Uri downloadUri { get; }

    protected BaseDownloadableTweak(HttpClient httpClient) {
        this.httpClient = httpClient;
    }

    public override async Task<Stream?> readFileAndEditIfNecessary(BaseTweakParams tweakParams) {
        return !File.Exists(tweakParams.filename) ? await httpClient.GetStreamAsync(downloadUri) : null;
    }

}