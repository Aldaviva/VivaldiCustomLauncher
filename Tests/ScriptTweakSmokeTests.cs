using System;
using System.IO;
using System.Threading.Tasks;
using VivaldiCustomLauncher.Tweaks;
using Xunit;
using Xunit.Sdk;

#nullable enable

namespace Tests {

    public class ScriptTweakSmokeTests {

        private const string ORIGINAL_BUNDLE_FILENAME                   = "Data/BundleScript/bundle.js";
        private const string ORIGINAL_BACKGROUND_COMMON_BUNDLE_FILENAME = "Data/BundleScript/background-common-bundle.js";

        private static readonly string ORIGINAL_BUNDLE_TEXT                   = File.ReadAllText(ORIGINAL_BUNDLE_FILENAME);
        private static readonly string ORIGINAL_BACKGROUND_COMMON_BUNDLE_TEXT = File.ReadAllText(ORIGINAL_BACKGROUND_COMMON_BUNDLE_FILENAME);

        private readonly BundleScriptTweak                 bundleTweak                 = new();
        private readonly BackgroundCommonBundleScriptTweak backgroundCommonBundleTweak = new();

        [Fact]
        public void originalBundleWasNotCustomized() {
            safeAssert(() => Assert.DoesNotContain("Customized by Ben", ORIGINAL_BUNDLE_TEXT), true, false);
        }

        [Fact]
        public void originalBackgroundBundleWasNotCustomized() {
            safeAssert(() => Assert.DoesNotContain("Customized by Ben", ORIGINAL_BACKGROUND_COMMON_BUNDLE_TEXT), true, false);
        }

        [Fact]
        public void exposeFolderSubscriptionStatusChangesBackgroundCommonBundle() {
            string actual = backgroundCommonBundleTweak.exposeFolderSubscriptionStatus(ORIGINAL_BACKGROUND_COMMON_BUNDLE_TEXT);
            safeAssert(() => Assert.NotEqual(ORIGINAL_BACKGROUND_COMMON_BUNDLE_TEXT, actual), false, false);
        }

        [Fact]
        public async Task closeTabOnBackGestureIfNoTabHistoryChangesBundle() {
            string actual = await bundleTweak.closeTabOnBackGestureIfNoTabHistory(ORIGINAL_BUNDLE_TEXT);
            safeAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
        }

        [Fact]
        public void removeExtraSpacingFromTabBarRightSideChangesBundle() {
            string actual = bundleTweak.removeExtraSpacingFromTabBarRightSide(ORIGINAL_BUNDLE_TEXT);
            safeAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
        }

        [Fact]
        public void increaseMaximumTabWidthChangesBundle() {
            string actual = bundleTweak.increaseMaximumTabWidth(ORIGINAL_BUNDLE_TEXT);
            safeAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
        }

        [Fact]
        public void formatDownloadProgressChangesBundle() {
            string actual = bundleTweak.formatDownloadProgress(ORIGINAL_BUNDLE_TEXT);
            safeAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
        }

        [Fact]
        public void navigateToSubdomainPartsChangesBundle() {
            string actual = bundleTweak.navigateToSubdomainParts(ORIGINAL_BUNDLE_TEXT);
            safeAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
        }

        [Fact]
        public void hideMailPanelHeadersChangesBundle() {
            string actual = bundleTweak.hideMailPanelHeaders(ORIGINAL_BUNDLE_TEXT);
            safeAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
        }

        [Fact]
        public void allowMovingMailBetweenAnyFoldersChangesBundle() {
            string actual = bundleTweak.allowMovingMailBetweenAnyFolders(ORIGINAL_BUNDLE_TEXT);
            safeAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
        }

        private static void safeAssert(Action assertion, bool preserveExpected = true, bool writeActualToTempFile = true) {
            try {
                assertion();
            } catch (AssertActualExpectedException e) {
                string expected = preserveExpected ? e.Expected : "(omitted)";
                string actual;

                if (writeActualToTempFile) {
                    string actualFileName = Path.GetTempFileName();
                    File.WriteAllText(actualFileName, e.Actual);
                    actual = $"(omitted, see {actualFileName})";
                } else {
                    actual = "(omitted)";
                }

                AssertActualExpectedException newException = (AssertActualExpectedException) (
                        e.GetType().GetConstructor(new[] { typeof(object), typeof(object) }) ??
                        e.GetType().GetConstructor(new[] { typeof(string), typeof(string) }))!
                    .Invoke(new object[] { expected, actual });

                throw newException;
            }
        }

    }

}