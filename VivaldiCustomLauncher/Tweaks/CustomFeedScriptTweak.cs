#nullable enable

using System;
using System.Net.Http;

namespace VivaldiCustomLauncher.Tweaks;

public class CustomFeedScriptTweak: BaseDownloadableTweak {

    protected override Uri downloadUri { get; } = new("https://github.com/Aldaviva/VivaldiCustomResources/raw/master/scripts/custom-feed.js");

    public CustomFeedScriptTweak(HttpClient httpClient): base(httpClient) { }

}