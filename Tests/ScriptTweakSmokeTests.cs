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

        [Fact]
        public void formatPhoneNumbersChangesBundle() {
            string actual = bundleTweak.formatPhoneNumbers(ORIGINAL_BUNDLE_TEXT);
            safeAssert(() => Assert.NotEqual(ORIGINAL_BUNDLE_TEXT, actual), false, false);
        }

        /// <summary>
        /// Don't print the actual or (optionally) expected strings, they might be several megabytes and freeze the unit test output
        /// </summary>
        /// <param name="assertion">an action that calls an assertion, like the following lambda: <c>() =&gt; Assert.Equal("a", "b")</c></param>
        /// <param name="preserveExpected"><c>true</c> if the expected value from the assertion should be printed, or <c>false</c> if <c>(omitted)</c> should be printed instead. You should set this parameter to <c>false</c> if the expected value is very long.</param>
        /// <param name="writeActualToTempFile">If <c>true</c> and the <c>assertion</c> fails, create a new empty temporary file on disk and write the actual value to it. Its filename will be printed in the exception message so you can open it and inspect it in your favorite text editor. If <c>false</c>, or if the <c>assertion</c> succeeds, don't create any file.</param>
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