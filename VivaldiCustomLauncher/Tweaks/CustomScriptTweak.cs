#nullable enable

using System;
using System.Net.Http;

namespace VivaldiCustomLauncher.Tweaks;

public class CustomScriptTweak: BaseDownloadableTweak {

    protected override Uri downloadUri { get; } = new(@"https://github.com/Aldaviva/VivaldiCustomResources/raw/master/scripts/custom.js");

    public CustomScriptTweak(HttpClient httpClient): base(httpClient) { }

}