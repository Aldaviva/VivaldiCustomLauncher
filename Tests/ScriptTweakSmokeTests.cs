#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Tests.Assertions;
using VivaldiCustomLauncher.Tweaks;
using Xunit;

namespace Tests;

[SuppressMessage("Microsoft.Design", "UnhandledExceptions:Unhandled exception(s)", Justification = "They're tests")]
public class ScriptTweakSmokeTests {

    private const string ORIGINAL_BUNDLE_FILENAME                   = "Data/BundleScript/bundle.js";
    private const string ORIGINAL_BACKGROUND_COMMON_BUNDLE_FILENAME = "Data/BundleScript/background-common-bundle.js";

    private static readonly string ORIGINAL_BUNDLE_TEXT                   = File.ReadAllText(ORIGINAL_BUNDLE_FILENAME);
    private static readonly string ORIGINAL_BACKGROUND_COMMON_BUNDLE_TEXT = File.ReadAllText(ORIGINAL_BACKGROUND_COMMON_BUNDLE_FILENAME);

    private readonly BundleScriptTweak                 bundleTweak                 = new();
    private readonly BackgroundCommonBundleScriptTweak backgroundCommonBundleTweak = new();

    [Fact]
    public void originalBundleWasNotCustomized() {
        FastAssert.fastAssert(() => Assert.DoesNotContain("Customized by Ben", ORIGINAL_BUNDLE_TEXT), true, false);
    }

    [Fact]
    public void originalBackgroundBundleWasNotCustomized() {
        FastAssert.fastAssert(() => Assert.DoesNotContain("Customized by Ben", ORIGINAL_BACKGROUND_COMMON_BUNDLE_TEXT), true, false);
    }

    [Fact]
    public void exposeFolderSubscriptionStatusChangesBackgroundCommonBundle() {
        string actual = backgroundCommonBundleTweak.exposeFolderSubscriptionStatus(ORIGINAL_BACKGROUND_COMMON_BUNDLE_TEXT);
        FastAssert.fastAssert(() => Assert.NotEqual(ORIGINAL_BACKGROUND_COMMON_BUNDLE_TEXT, actual), false, false);
    }

    [Fact]
    public async Task closeTabOnBackGestureIfNoTabHistoryChangesBundle() {
        string actual = await bundleTweak.closeTabOnBackGestureIfNoTabHistory(ORIGINAL_BUNDLE_TEXT);
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

    [Fact(Skip = "unused")]
    [Obsolete]
    public void formatPhoneNumbersChangesBundle() {
        string actual = bundleTweak.formatPhoneNumbers(ORIGINAL_BUNDLE_TEXT);
        FastAssert.fastAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
    }

    [Fact(Skip = "mainlined")]
    public void formatCalendarAgendaDatesChangesBundle() {
        string actual = bundleTweak.formatCalendarAgendaDates(ORIGINAL_BUNDLE_TEXT);
        FastAssert.fastAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
    }

    [Fact]
    public void disableAutoHeightForImagesInMailWithHeightAttributeChangesBundle() {
        string actual = bundleTweak.disableAutoHeightForImagesInMailWithHeightAttribute(ORIGINAL_BUNDLE_TEXT);
        FastAssert.fastAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
    }

    [Fact]
    public void classifyJunkEmailAsNormalFolder() {
        string actual = backgroundCommonBundleTweak.classifyJunkEmailAsNormalFolder(ORIGINAL_BACKGROUND_COMMON_BUNDLE_TEXT);
        FastAssert.fastAssert(() => Assert.NotEqual(ORIGINAL_BACKGROUND_COMMON_BUNDLE_TEXT, actual), false, false);
    }

}