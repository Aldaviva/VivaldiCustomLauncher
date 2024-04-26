#nullable enable

using Tests.Assertions;
using Tests.Data;
using VivaldiCustomLauncher.Tweaks;
using Xunit;

namespace Tests;

public class ScriptTweakSmokeTests {

    private static readonly string ORIGINAL_BUNDLE_TEXT            = DataReader.readFileTextForCurrentBuildType("BundleScript/bundle.js");
    private static readonly string ORIGINAL_BACKGROUND_BUNDLE_TEXT = DataReader.readFileTextForCurrentBuildType("BundleScript/background-bundle.js");

    private readonly BundleScriptTweak           bundleTweak           = new();
    private readonly BackgroundBundleScriptTweak backgroundBundleTweak = new();

    [Fact]
    public void originalBundleWasNotCustomized() {
        FastAssert.fastAssert(() => Assert.DoesNotContain("Customized by Ben", ORIGINAL_BUNDLE_TEXT), true, false);
    }

    [Fact]
    public void originalBackgroundBundleWasNotCustomized() {
        FastAssert.fastAssert(() => Assert.DoesNotContain("Customized by Ben", ORIGINAL_BACKGROUND_BUNDLE_TEXT), true, false);
    }

    [Fact]
    public void closeTabOnBackGestureIfNoTabHistoryChangesBundle() {
        string actual = bundleTweak.closeTabOnBackGestureIfNoTabHistory(ORIGINAL_BUNDLE_TEXT);
        FastAssert.fastAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
    }

    [Fact]
    public void removeExtraSpacingFromTabBarRightSideChangesBundle() {
        string actual = bundleTweak.removeExtraSpacingFromTabBarRightSide(ORIGINAL_BUNDLE_TEXT);
        FastAssert.fastAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
    }

    [Fact]
    public void increaseMaximumTabWidthChangesBundle() {
        string actual = bundleTweak.increaseMaximumTabWidth(ORIGINAL_BUNDLE_TEXT);
        FastAssert.fastAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
    }

    [Fact]
    public void formatDownloadProgressChangesBundle() {
        string actual = bundleTweak.formatDownloadProgress(ORIGINAL_BUNDLE_TEXT);
        FastAssert.fastAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
    }

    [Fact]
    public void navigateToSubdomainPartsChangesBundle() {
        string actual = bundleTweak.navigateToSubdomainParts(ORIGINAL_BUNDLE_TEXT);
        FastAssert.fastAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
    }

    [Fact]
    public void allowMovingMailBetweenAnyFoldersChangesBundle() {
        string actual = bundleTweak.allowMovingMailBetweenAnyFolders(ORIGINAL_BUNDLE_TEXT);
        FastAssert.fastAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
    }

    [Fact]
    public void classifyJunkEmailAsNormalFolder() {
        string actual = backgroundBundleTweak.classifyJunkEmailAsNormalFolder(ORIGINAL_BACKGROUND_BUNDLE_TEXT);
        FastAssert.fastAssert(() => Assert.NotEqual(ORIGINAL_BACKGROUND_BUNDLE_TEXT, actual), false, false);
    }

    [Fact]
    public void expandDomainsWithHttps() {
        string actual = bundleTweak.expandDomainsWithHttps(ORIGINAL_BUNDLE_TEXT);
        FastAssert.fastAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
    }

}