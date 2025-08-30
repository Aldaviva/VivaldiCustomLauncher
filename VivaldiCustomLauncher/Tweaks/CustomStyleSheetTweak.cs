#nullable enable

using System;
using System.Net.Http;

namespace VivaldiCustomLauncher.Tweaks;

public class CustomStyleSheetTweak(HttpClient httpClient): BaseDownloadableTweak(httpClient) {

    protected override Uri downloadUri { get; } = new("https://github.com/Aldaviva/VivaldiCustomResources/raw/master/style/custom.css");

}