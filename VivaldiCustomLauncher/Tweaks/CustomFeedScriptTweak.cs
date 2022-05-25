#nullable enable

using System;
using System.Net.Http;

namespace VivaldiCustomLauncher.Tweaks;

public class CustomFeedScriptTweak: BaseDownloadableTweak {

    protected override Uri downloadUri { get; } = new("https://github.com/Aldaviva/VivaldiCustomResources/raw/master/scripts/showfeed-custom.js");

    public CustomFeedScriptTweak(HttpClient httpClient): base(httpClient) { }

}