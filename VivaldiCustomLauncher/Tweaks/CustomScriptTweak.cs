#nullable enable

namespace VivaldiCustomLauncher.Tweaks;

public class CustomScriptTweak(HttpClient httpClient): BaseDownloadableTweak(httpClient) {

    protected override Uri downloadUri { get; } = new("https://raw.githubusercontent.com/Aldaviva/VivaldiCustomResources/master/scripts/custom.js");

}