#nullable enable

namespace VivaldiCustomLauncher.Tweaks;

public class CustomFeedScriptTweak(HttpClient httpClient): BaseDownloadableTweak(httpClient) {

    protected override Uri downloadUri { get; } = new("https://raw.githubusercontent.com/Aldaviva/VivaldiCustomResources/master/scripts/custom-feed.js");

}