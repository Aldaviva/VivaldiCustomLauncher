using System.IO;
using System.Threading.Tasks;
using VivaldiCustomLauncher.Tweaks;
using Xunit;
using Xunit.Sdk;

#nullable enable

namespace Tests {

    public class TweakBundleScriptTest {

        private const           string ORIGINAL_BUNDLE_FILENAME = "Data/BundleScript/original-bundle.js";
        private static readonly string ORIGINAL_BUNDLE_TEXT     = File.ReadAllText(ORIGINAL_BUNDLE_FILENAME);

        private readonly BundleScriptTweak tweak = new();

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
            string actual = tweak.removeExtraSpacingFromTabBarRightSide(ORIGINAL_BUNDLE_TEXT);

            const string EXPECTED =
                @"Y(this,""getStyles"",()=>{const e=this.props.prefValues[R.kTabsThumbnails],t=this.createFlexBoxLayout(this.props.tabs,this.props.direction,this.props.maxWidth+62/* Customized by Ben */,this.props.maxHeight,{";

            safeAssertReplacement(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
        }

        [Fact]
        public void increaseMaximumTabWidth() {
            string actual = tweak.increaseMaximumTabWidth(ORIGINAL_BUNDLE_TEXT);

            const string EXPECTED = @"return t?(r.maxWidth=4000/* Customized by Ben */,r.maxHeight=";

            safeAssertReplacement(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
        }

        /*
         * TODO: this test is very brittle because the expected value changes with each Vivaldi version.
         * Specifically, the numeric dependency ID (160) and the dependency variables (Me, f) change.
         * Maybe the test should use a regular expression match instead of Assert.Contains, which is harder to guarantee perfect results for one Vivaldi version, but is easier to deal with different versions.
         */
        [Fact]
        public async Task closeTabOnBackGestureIfNoTabHistory() {
            string actual = await tweak.closeTabOnBackGestureIfNoTabHistory(ORIGINAL_BUNDLE_TEXT);

            const string EXPECTED =
                @"{name:""COMMAND_PAGE_BACK"",action:() => { const activePage = h.c.getActivePage(), navigationInfo = activePage && a(152).a.getNavigationInfo(activePage.id); navigationInfo && navigationInfo.canGoBack ? fe.a.back() : d.a.close() } /* Customized by Ben */,";

            safeAssertReplacement(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
        }

        [Fact]
        public void formatDownloadProgress() {
            string actual = tweak.formatDownloadProgress(ORIGINAL_BUNDLE_TEXT);

            const string EXPECTED =
                @"u=function(e,t,a){return e&&t>a?m()(t)/* Customized by Ben */.fromNow(true):e&&t<=a?Object(r.a)(""about a second""):""""}(n,a,t),h=Object(r.a)(""$1/s"",[Object(c.a)(Object(d.a)(e))]),p=e.paused||e.state===E;return i.a.createElement(""div"",{className:""DownloadItem-FileSize"",__source:{fileName:""D:\\builder\\workers\\ow64\\build\\vivaldi\\vivapp\\src\\components\\downloads\\DownloadPanel\\DownloadSize.jsx"",lineNumber:60,columnNumber:5}},u&&`${u}, `,e.state===v?l:p?Object(r.a)(""$1/$2 - stopped"",[o,l]):Object(r.a)(""$3, $1/$2"",[o,l,h]))}";

            safeAssertReplacement(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
        }

        [Fact]
        public void navigateToSubdomainParts() {
            string actual = tweak.navigateToSubdomainParts(ORIGINAL_BUNDLE_TEXT);

            string expected =
                @",s&&this.props.subdomain.split(""."").map((part, index, whole) => n.createElement(""span"", { className: ""UrlFragment--Lowlight UrlFragment-HostFragment-Subdomain"", onClick: e => { e.stopPropagation(); this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : """") + whole.slice(index).join(""."") + ""."" + this.props.basedomain + ""."" + this.props.tld + (this.props.port ? `:${this.props.port}` : """")); }}, part, ""."")) /* Customized by Ben */,n.createElement";
            safeAssertReplacement(ORIGINAL_BUNDLE_TEXT, actual, expected);

            expected =
                @".createElement(""span"",{className:""UrlFragment--Highlight UrlFragment-HostFragment-Basedomain"",onClick: e => { e.stopPropagation(); this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : """") + this.props.basedomain + ""."" + this.props.tld + (this.props.port ? `:${this.props.port}` : """")); } /* Customized by Ben */,";
            safeAssertReplacement(ORIGINAL_BUNDLE_TEXT, actual, expected);

            expected =
                @".createElement(""span"",{className:""UrlFragment--Highlight UrlFragment-HostFragment-TLD"",onClick: e => { e.stopPropagation(); this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : """") + this.props.basedomain + ""."" + this.props.tld + (this.props.port ? `:${this.props.port}` : """")); } /* Customized by Ben */,";
            safeAssertReplacement(ORIGINAL_BUNDLE_TEXT, actual, expected);

            expected =
                @".createElement(""span"",{className:""UrlFragment--Lowlight UrlFragment-HostFragment-Port"",onClick: e => { e.stopPropagation(); this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : """") + this.props.basedomain + ""."" + this.props.tld + (this.props.port ? `:${this.props.port}` : """")); } /* Customized by Ben */,";
            safeAssertReplacement(ORIGINAL_BUNDLE_TEXT, actual, expected);
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