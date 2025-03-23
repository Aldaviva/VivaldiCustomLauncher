#nullable enable

using System;
using System.Net.Http;

namespace VivaldiCustomLauncher.Tweaks;

public class ModStyleSheetTweak(HttpClient httpClient): BaseDownloadableTweak(httpClient) {

    protected override Uri downloadUri { get; } = new("https://github.com/Aldaviva/VivaldiCustomResources/raw/master/style/mods.css");

}