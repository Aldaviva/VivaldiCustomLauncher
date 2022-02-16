using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Tests.Assertions;
using VivaldiCustomLauncher.Tweaks;
using Xunit;
using Xunit.Sdk;

#nullable enable

namespace Tests {

    [SuppressMessage("Microsoft.Design", "UnhandledExceptions:Unhandled exception(s)", Justification = "They're tests")]
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
                @"this.createFlexBoxLayout(this.props.tabs,this.props.direction,this.props.maxWidth+62/* Customized by Ben */,this.props.maxHeight,{";

            FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
        }

        [Fact]
        public void increaseMaximumTabWidth() {
            string actual = tweak.increaseMaximumTabWidth(ORIGINAL_BUNDLE_TEXT);

            const string EXPECTED = @"const l8=4000/* Customized by Ben */,";

            FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
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
                @"{name:""COMMAND_PAGE_BACK"",action:e=>{const t=b.ZP.getActivePage(e.windowId);/* Customized by Ben */const navigationInfo = t && n(1877).Z.getNavigationInfo(t.id);if(!navigationInfo || navigationInfo.canGoBack){t&&ye.Z.back(e.windowId,t.id)} else {g.Z.close();}},";

            FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
        }

        [Fact]
        public void formatDownloadProgress() {
            string actual = tweak.formatDownloadProgress(ORIGINAL_BUNDLE_TEXT);

            const string EXPECTED =
                @"return e&&t>n?g()(t).fromNow(true)/* Customized by Ben */:e&&t<=n?(0,fe.Z)(""1 second""):""""}(s,n,t),c=(0,fe.Z)(""$1/s"",[Or(e.currentSpeed)]),u=e.paused||e.state===Ef,m=e.error?function(e){switch(e){case""FILE_NO_SPACE"":return(0,fe.Z)(""Disk is full"");case""FILE_TOO_LARGE"":return(0,fe.Z)(""File too large"");case""FILE_FAILED"":return(0,fe.Z)(""Download error"");case""FILE_ACCESS_DENIED"":return(0,fe.Z)(""Insufficient permissions"");case""FILE_NAME_TOO_LONG"":return(0,fe.Z)(""Path too long"");case""FILE_VIRUS_INFECTED"":return(0,fe.Z)(""Virus detected"");case""FILE_TRANSIENT_ERROR"":return(0,fe.Z)(""System busy"");case""FILE_BLOCKED"":return(0,fe.Z)(""Blocked"");case""FILE_SECURITY_CHECK_FAILED"":return(0,fe.Z)(""Virus scan failed"");case""FILE_TOO_SHORT"":return(0,fe.Z)(""File truncated"");case""FILE_SAME_AS_SOURCE"":return(0,fe.Z)(""Already downloaded"");case""FILE_HASH_MISMATCH"":return(0,fe.Z)(""Hash mismatch"");case""NETWORK_FAILED"":return(0,fe.Z)(""Network"");case""NETWORK_TIMEOUT"":return(0,fe.Z)(""Timeout"");case""NETWORK_DISCONNECTED"":return(0,fe.Z)(""Disconnected"");case""NETWORK_SERVER_DOWN"":return(0,fe.Z)(""Server unavailable"");case""NETWORK_INVALID_REQUEST"":return(0,fe.Z)(""Invalid network request"");case""SERVER_FAILED"":return(0,fe.Z)(""Server failed"");case""SERVER_NO_RANGE"":return(0,fe.Z)(""Server does not support range"");case""SERVER_BAD_CONTENT"":return(0,fe.Z)(""The server could not find the file"");case""SERVER_UNAUTHORIZED"":return(0,fe.Z)(""Unauthorized"");case""SERVER_CERT_PROBLEM"":return(0,fe.Z)(""Certificate problem"");case""SERVER_FORBIDDEN"":return(0,fe.Z)(""Forbidden"");case""SERVER_UNREACHABLE"":return(0,fe.Z)(""Unreachable"");case""SERVER_CONTENT_LENGTH_MISMATCH"":return(0,fe.Z)(""Content length mismatch"");case""SERVER_CROSS_ORIGIN_REDIRECT"":return(0,fe.Z)(""Cross origin redirect"");case""USER_CANCELED"":return(0,fe.Z)(""Cancelled"");case""USER_SHUTDOWN"":return(0,fe.Z)(""Shutdown"");case""CRASH"":return(0,fe.Z)(""Crashed"")}return e}(e.error):"""",d=u?(0,fe.Z)(""$1/$2 - stopped"",[a,o]):(0,fe.Z)(""$3, $1/$2"",[a,o,c]);return i.createElement(""div"",{className:""DownloadItem-FileSize"",__source:{fileName:""D:\\builder\\workers\\ow64\\build\\vivaldi\\vivapp\\src\\components\\downloads\\DownloadPanel\\DownloadSize.jsx"",lineNumber:159,columnNumber:5}},l&&`${l}, `,e.state===vf?o:e.error?m:d)/* Customized by Ben */}";

            FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
        }

        [Fact]
        public void navigateToSubdomainParts() {
            string actual = tweak.navigateToSubdomainParts(ORIGINAL_BUNDLE_TEXT);

            string expected =
                @"&&this.props.subdomain.split(""."").map((part, index, whole) => i.createElement(""span"", { className: ""UrlFragment--Lowlight UrlFragment-HostFragment-Subdomain"", onClick: e => { e.stopPropagation(); this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : """") + whole.slice(index).join(""."") + ""."" + this.props.basedomain + ""."" + this.props.tld + (this.props.port ? `:${this.props.port}` : """")); }}, part, ""."")) /* Customized by Ben */,";
            FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, expected);

            expected =
                @".createElement(""span"",{className:""UrlFragment--Highlight UrlFragment-HostFragment-Basedomain"",onClick: e => { e.stopPropagation(); this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : """") + this.props.basedomain + ""."" + this.props.tld + (this.props.port ? `:${this.props.port}` : """")); } /* Customized by Ben */,";
            FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, expected);

            expected =
                @".createElement(""span"",{className:""UrlFragment--Highlight UrlFragment-HostFragment-TLD"",onClick: e => { e.stopPropagation(); this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : """") + this.props.basedomain + ""."" + this.props.tld + (this.props.port ? `:${this.props.port}` : """")); } /* Customized by Ben */,";
            FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, expected);

            expected =
                @".createElement(""span"",{className:""UrlFragment--Lowlight UrlFragment-HostFragment-Port"",onClick: e => { e.stopPropagation(); this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : """") + this.props.basedomain + ""."" + this.props.tld + (this.props.port ? `:${this.props.port}` : """")); } /* Customized by Ben */,";
            FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, expected);
        }

        [Fact]
        public void formatPhoneNumber() {
            string actual = tweak.formatPhoneNumbers(ORIGINAL_BUNDLE_TEXT);

            const string EXPECTED =
                @"(this,""addSpaces"",raw => {const digits = raw.replace(/[^0-9a-z]/ig, '');switch (digits.length){case 7:  return digits.substr(0,3) + '-' + digits.substr(3,4);case 10:  return '(' + digits.substr(0,3) + ') ' + digits.substr(3,3) + '-' + digits.substr(6,4);case 11:  return digits[0] + ' (' + digits.substr(1,3) + ') ' + digits.substr(4,3) + '-' + digits.substr(7,4);default:  return digits;}}/* Customized by Ben */),";

            FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
        }

        [Fact]
        public void formatCalendarAgendaDates() {
            string actual = tweak.formatCalendarAgendaDates(ORIGINAL_BUNDLE_TEXT);

            const string EXPECTED =
                @"i.createElement(""div"",{className:""cal-tasks-row-date"",__source:{fileName:LY,lineNumber:358,columnNumber:13}},g()(r).format(""ddd, MMM D, YYYY""/* Customized by Ben */)),";

            FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
        }

        [Fact]
        public void hideMailPanelHeaders() {
            string actual = tweak.hideMailPanelHeaders(ORIGINAL_BUNDLE_TEXT);

            const string EXPECTED =
                @",u=c?[O4]:l.concat(o).slice(7)/* Customized by Ben */,";

            FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
        }

        [Fact]
        public void allowMovingMailBetweenAnyFolders() {
            string actual = tweak.allowMovingMailBetweenAnyFolders(ORIGINAL_BUNDLE_TEXT);

            const string EXPECTED =
                @"])=>{const typesOrdered = [""Inbox"", ""Drafts"", ""Sent"", ""Archive"", ""Trash"", ""Junk"", ""Other""];let i = Object.entries(c.Z.getFolders()[t]).filter(([path, folder]) => folder.subscribed).sort(([pathA, folderA], [pathB, folderB]) => (folderA.type === folderB.type) ? pathA.localeCompare(pathB) : typesOrdered.indexOf(folderA.type) - typesOrdered.indexOf(folderB.type)).map(([path, folder]) => path);i=i.filter((e=>e!==n)),i.length>0&&s.push(...i.map((i=>({handler:()=>W(e,t,n,i),...(0,ie.Z)(i)}))))/* Customized by Ben */})),";

            FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
        }

    }

}