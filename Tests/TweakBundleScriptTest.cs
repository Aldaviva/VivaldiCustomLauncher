using System.IO;
using System.Threading.Tasks;
using VivaldiCustomLauncher.Tweaks;
using Xunit;
using Xunit.Sdk;

#nullable enable

namespace Tests {

    public class TweakBundleScriptTest {

        private const string ORIGINAL_BUNDLE_FILENAME = "Data/BundleScript/original-bundle.js";

        private readonly BundleScriptTweak tweak = new BundleScriptTweak();

        [Fact]
        public void originalBundleWasNotCustomized() {
            string input = File.ReadAllText(ORIGINAL_BUNDLE_FILENAME);

            try {
                Assert.DoesNotContain("Customized by Ben", input);
            } catch (DoesNotContainException e) {
                throw new DoesNotContainException(e.Expected, "(omitted)");
            }
        }

        [Fact]
        public void removeExtraSpacingFromTabBarRightSide() {
            string input = File.ReadAllText(ORIGINAL_BUNDLE_FILENAME);

            string actual = tweak.removeExtraSpacingFromTabBarRightSide(input);

            const string EXPECTED = @"(this,""getStyles"",e=>this.createFlexBoxLayout(this.props.tabs,this.props.direction,this.props.maxWidth+62/* Customized by Ben */,this.props.maxHeight,{";

            safeAssertReplacement(input, actual, EXPECTED);
        }

        [Fact]
        public void increaseMaximumTabWidth() {
            string input = File.ReadAllText(ORIGINAL_BUNDLE_FILENAME);

            string actual = tweak.increaseMaximumTabWidth(input);

            const string EXPECTED = @"return t?(r.maxWidth=4000/* Customized by Ben */,r.maxHeight=";

            safeAssertReplacement(input, actual, EXPECTED);
        }

        /*
         * TODO: this test is very brittle because the expected value changes with each Vivaldi version.
         * Specifically, the numeric dependency ID (160) and the dependency variables (Me, f) change.
         * Maybe the test should use a regular expression match instead of Assert.Contains, which is harder to guarantee perfect results for one Vivaldi version, but is easier to deal with different versions.
         */
        [Fact]
        public async Task closeTabOnBackGestureIfNoTabHistory() {
            string input = File.ReadAllText(ORIGINAL_BUNDLE_FILENAME);

            string actual = await tweak.closeTabOnBackGestureIfNoTabHistory(input);

            const string EXPECTED =
                @"{name:""COMMAND_PAGE_BACK"",action:()=>{const c=_.b.getActivePage(),e=c&&a(160).a.getNavigationInfo(c.id);e&&e.canGoBack?Me.a.back():f.a.close()}/* Customized by Ben */,category:";

            safeAssertReplacement(input, actual, EXPECTED);
        }

        [Fact]
        public void formatDownloadProgress() {
            string input = File.ReadAllText(ORIGINAL_BUNDLE_FILENAME);

            string actual = tweak.formatDownloadProgress(input);

            const string EXPECTED =
                @"u=function(e,t,a){return e&&t>a?m()(t)/* Customized by Ben */.fromNow(true):e&&t<=a?Object(r.a)(""about a second""):""""}(n,a,t),h=Object(r.a)(""$1/s"",[Object(c.a)(Object(d.a)(e))]),p=e.paused||e.state===E;return i.a.createElement(""div"",{className:""DownloadItem-FileSize"",__source:{fileName:""D:\\builder\\workers\\ow64\\build\\vivaldi\\vivapp\\src\\components\\downloads\\DownloadPanel\\DownloadSize.jsx"",lineNumber:60,columnNumber:5}},u&&`${u}, `,e.state===v?l:p?Object(r.a)(""$1/$2 - stopped"",[o,l]):Object(r.a)(""$3, $1/$2"",[o,l,h]))}";

            safeAssertReplacement(input, actual, EXPECTED);
        }

        private static void safeAssertReplacement(string originalInput, string actualInput, string expected) {
            try {
                Assert.DoesNotContain(expected, originalInput);
            } catch (DoesNotContainException e) {
                throw new DoesNotContainException(e.Expected, "(too large, omitted)");
            }

            try {
                Assert.Contains(expected, actualInput);
            } catch (ContainsException e) {
                string actualFileName = Path.GetTempFileName();
                File.WriteAllText(actualFileName, actualInput);
                throw new ContainsException(e.Expected, $"(too large, see {actualFileName})");
            }
        }

    }

}