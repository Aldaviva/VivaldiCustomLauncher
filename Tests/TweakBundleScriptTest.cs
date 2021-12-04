using System.IO;
using System.Threading.Tasks;
using VivaldiCustomLauncher.Tweaks;
using Xunit;
using Xunit.Sdk;

#nullable enable

namespace Tests {

    public class TweakBundleScriptTest {

        private const           string ORIGINAL_BUNDLE_FILENAME = "Data/BundleScript/bundle.js";
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
                @"l=function(e,t,n){return e&&t>n?g()(t).fromNow(true)/* Customized by Ben */:e&&t<=n?(0,ue.Z)(""1 second""):""""}(i,n,t),c=(0,ue.Z)(""$1/s"",[Cr(e.currentSpeed)]),u=e.paused||e.state===oN,m=e.error?function(e){switch(e){case""FILE_NO_SPACE"":return(0,ue.Z)(""Disk is full"");case""FILE_TOO_LARGE"":return(0,ue.Z)(""File too large"");case""FILE_FAILED"":return(0,ue.Z)(""Download error"");case""FILE_ACCESS_DENIED"":return(0,ue.Z)(""Insufficient permissions"");case""FILE_NAME_TOO_LONG"":return(0,ue.Z)(""Path too long"");case""FILE_VIRUS_INFECTED"":return(0,ue.Z)(""Virus detected"");case""FILE_TRANSIENT_ERROR"":return(0,ue.Z)(""System busy"");case""FILE_BLOCKED"":return(0,ue.Z)(""Blocked"");case""FILE_SECURITY_CHECK_FAILED"":return(0,ue.Z)(""Virus scan failed"");case""FILE_TOO_SHORT"":return(0,ue.Z)(""File truncated"");case""FILE_SAME_AS_SOURCE"":return(0,ue.Z)(""Already downloaded"");case""FILE_HASH_MISMATCH"":return(0,ue.Z)(""Hash mismatch"");case""NETWORK_FAILED"":return(0,ue.Z)(""Network"");case""NETWORK_TIMEOUT"":return(0,ue.Z)(""Timeout"");case""NETWORK_DISCONNECTED"":return(0,ue.Z)(""Disconnected"");case""NETWORK_SERVER_DOWN"":return(0,ue.Z)(""Server unavailable"");case""NETWORK_INVALID_REQUEST"":return(0,ue.Z)(""Invalid network request"");case""SERVER_FAILED"":return(0,ue.Z)(""Server failed"");case""SERVER_NO_RANGE"":return(0,ue.Z)(""Server does not support range"");case""SERVER_BAD_CONTENT"":return(0,ue.Z)(""The server could not find the file"");case""SERVER_UNAUTHORIZED"":return(0,ue.Z)(""Unauthorized"");case""SERVER_CERT_PROBLEM"":return(0,ue.Z)(""Certificate problem"");case""SERVER_FORBIDDEN"":return(0,ue.Z)(""Forbidden"");case""SERVER_UNREACHABLE"":return(0,ue.Z)(""Unreachable"");case""SERVER_CONTENT_LENGTH_MISMATCH"":return(0,ue.Z)(""Content length mismatch"");case""SERVER_CROSS_ORIGIN_REDIRECT"":return(0,ue.Z)(""Cross origin redirect"");case""USER_CANCELED"":return(0,ue.Z)(""Cancelled"");case""USER_SHUTDOWN"":return(0,ue.Z)(""Shutdown"");case""CRASH"":return(0,ue.Z)(""Crashed"")}return e}(e.error):"""",d=u?(0,ue.Z)(""$1/$2 - stopped"",[a,o]):(0,ue.Z)(""$3, $1/$2"",[a,o,c]);return s.createElement(""div"",{className:""DownloadItem-FileSize"",__source:{fileName:""D:\\builder\\workers\\ow64\\build\\vivaldi\\vivapp\\src\\components\\downloads\\DownloadPanel\\DownloadSize.jsx"",lineNumber:159,columnNumber:5}},l&&`${l}, `,e.state===aN?o:e.error?m:d)/* Customized by Ben */}";

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