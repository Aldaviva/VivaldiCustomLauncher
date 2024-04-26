#nullable enable

using System;
using System.Net.Http;

namespace VivaldiCustomLauncher.Tweaks;

public class CustomScriptTweak(HttpClient httpClient): BaseDownloadableTweak(httpClient) {

    protected override Uri downloadUri { get; } = new("https://github.com/Aldaviva/VivaldiCustomResources/raw/master/scripts/custom.js");

}