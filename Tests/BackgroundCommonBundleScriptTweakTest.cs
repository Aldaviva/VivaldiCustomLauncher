using System.Diagnostics.CodeAnalysis;
using System.IO;
using Tests.Assertions;
using VivaldiCustomLauncher.Tweaks;
using Xunit;
using Xunit.Sdk;

#nullable enable

namespace Tests {

    [SuppressMessage("Microsoft.Design", "UnhandledExceptions:Unhandled exception(s)", Justification = "They're tests")]
    public class BackgroundCommonBundleScriptTweakTest {

        private const           string ORIGINAL_BUNDLE_FILENAME = "Data/BundleScript/background-common-bundle.js";
        private static readonly string ORIGINAL_BUNDLE_TEXT     = File.ReadAllText(ORIGINAL_BUNDLE_FILENAME);

        private readonly BackgroundCommonBundleScriptTweak tweak = new();

        [Fact]
        public void originalBundleWasNotCustomized() {
            try {
                Assert.DoesNotContain("Customized by Ben", ORIGINAL_BUNDLE_TEXT);
            } catch (DoesNotContainException e) {
                throw new DoesNotContainException(e.Expected, "(omitted)");
            }
        }

        [Fact]
        public void removeExtraSpacingFromTabBarRightSide() {
            string actual = tweak.exposeFolderSubscriptionStatus(ORIGINAL_BUNDLE_TEXT);

            const string EXPECTED =
                @"]={type:n,subscribed:t.subscribed/* Customized by Ben */}}";

            FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
        }

    }

}