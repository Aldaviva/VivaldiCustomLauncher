#nullable enable

namespace VivaldiCustomLauncher.Tweaks;

public class ModStyleSheetTweak(HttpClient httpClient): BaseDownloadableTweak(httpClient) {

    protected override Uri downloadUri { get; } = new("https://raw.githubusercontent.com/Aldaviva/VivaldiCustomResources/master/style/mods.css");

}