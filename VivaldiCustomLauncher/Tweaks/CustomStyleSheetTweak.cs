#nullable enable

namespace VivaldiCustomLauncher.Tweaks;

public class CustomStyleSheetTweak(HttpClient httpClient): BaseDownloadableTweak(httpClient) {

    protected override Uri downloadUri { get; } = new("https://raw.githubusercontent.com/Aldaviva/VivaldiCustomResources/master/style/custom.css");

}