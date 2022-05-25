#nullable enable

using System;
using System.Net.Http;

namespace VivaldiCustomLauncher.Tweaks;

public class CustomStyleSheetTweak: BaseDownloadableTweak {

    protected override Uri downloadUri { get; } = new(@"https://github.com/Aldaviva/VivaldiCustomResources/raw/master/style/custom.css");

    public CustomStyleSheetTweak(HttpClient httpClient): base(httpClient) { }

}