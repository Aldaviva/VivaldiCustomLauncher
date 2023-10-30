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

    public override async Task<Stream> readAndEditFile(BaseTweakParams tweakParams) {
        try {
            return await httpClient.GetStreamAsync(downloadUri);
        } catch (HttpRequestException e) {
            e.Data["uri"] = downloadUri;
            throw;
        }
    }

}