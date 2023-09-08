#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.Core.Internal;
using FakeItEasy;
using Tests.Assertions;
using Tests.Data;
using VivaldiCustomLauncher.Tweaks;
using Xunit;
using Xunit.Sdk;

namespace Tests;

public class BundleScriptTweakTest {

    private static readonly string ORIGINAL_BUNDLE_TEXT = DataReader.readFileTextForCurrentBuildType("BundleScript/bundle.js");

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
    public async Task allTweaksAreCalled() {
        IList<string>     fakedCalls = new List<string>();
        BundleScriptTweak tweakSpy   = A.Fake<BundleScriptTweak>();

        // for this to work, the methods being called by editFile() must be virtual, and the assembly under test must have InternalsVisibleTo DynamicProxyGenAssembly2 (since the methods are internal, not public)
        A.CallTo(tweakSpy).Invokes(call => fakedCalls.Add(call.Method.Name));
        A.CallTo(() => tweakSpy.editFile(A<string>._)).CallsBaseMethod();

        await tweakSpy.editFile(ORIGINAL_BUNDLE_TEXT);

        IEnumerable<MethodInfo> expectedMethods = typeof(BundleScriptTweak)
            .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(method => method.Name != nameof(BundleScriptTweak.editFile) && method.GetAttribute<ObsoleteAttribute>() is null)
            .ToList();

        Assert.NotEmpty(expectedMethods);
        foreach (MethodInfo expectedMethod in expectedMethods) {
            // If this assertion fails but editFile() is actually calling the expected method, make sure the expected method is virtual, so that FakeItEasy can proxy it and detect it being called
            Assert.Contains(expectedMethod.Name, fakedCalls);
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

        const string EXPECTED = @"=5200/* Customized by Ben */,";

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
    }

    /*
     * TODO: this test is very brittle because the expected value changes with each Vivaldi version.
     * Specifically, the numeric dependency ID (160) and the dependency variables (Me, f) change.
     * Maybe the test should use a regular expression match instead of Assert.Contains, which is harder to guarantee perfect results for one Vivaldi version, but is easier to deal with different versions.
     */
    [Fact]
    public void closeTabOnBackGestureIfNoTabHistory() {
        string actual = tweak.closeTabOnBackGestureIfNoTabHistory(ORIGINAL_BUNDLE_TEXT);

        const string EXPECTED =
            @"{name:""COMMAND_PAGE_BACK"",action:e=>{const t=w.Z.getActivePage(e.windowId);/* Customized by Ben */if(document.querySelector('.webpageview.active:not(:has(#mail_view)) webview')?.canGoBack() ?? false){t&&L.Z.back(e.windowId,t.id)} else {y.ZP.close(e.windowId);}},";

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
    }

    [Fact]
    public void formatDownloadProgress() {
        string actual = tweak.formatDownloadProgress(ORIGINAL_BUNDLE_TEXT);

        const string EXPECTED =
            @"return e&&t>n?b()(t).fromNow(true)/* Customized by Ben */:e&&t<=n?(0,le.Z)(""1 second""):""""}(i,n,t),l=(0,le.Z)(""$1/s"",[Co(e.currentSpeed)]),d=e.paused||e.state===Xp,c=e.error?function(e){switch(e){case""FILE_NO_SPACE"":return(0,le.Z)(""Disk is full"");case""FILE_TOO_LARGE"":return(0,le.Z)(""File too large"");case""FILE_FAILED"":return(0,le.Z)(""Download error"");case""FILE_ACCESS_DENIED"":return(0,le.Z)(""Insufficient permissions"");case""FILE_NAME_TOO_LONG"":return(0,le.Z)(""Path too long"");case""FILE_VIRUS_INFECTED"":return(0,le.Z)(""Virus detected"");case""FILE_TRANSIENT_ERROR"":return(0,le.Z)(""System busy"");case""FILE_BLOCKED"":return(0,le.Z)(""Blocked"");case""FILE_SECURITY_CHECK_FAILED"":return(0,le.Z)(""Virus scan failed"");case""FILE_TOO_SHORT"":return(0,le.Z)(""File truncated"");case""FILE_SAME_AS_SOURCE"":return(0,le.Z)(""Already downloaded"");case""FILE_HASH_MISMATCH"":return(0,le.Z)(""Hash mismatch"");case""NETWORK_FAILED"":return(0,le.Z)(""Network"");case""NETWORK_TIMEOUT"":return(0,le.Z)(""Timeout"");case""NETWORK_DISCONNECTED"":return(0,le.Z)(""Disconnected"");case""NETWORK_SERVER_DOWN"":return(0,le.Z)(""Server unavailable"");case""NETWORK_INVALID_REQUEST"":return(0,le.Z)(""Invalid network request"");case""SERVER_FAILED"":return(0,le.Z)(""Server failed"");case""SERVER_NO_RANGE"":return(0,le.Z)(""Server does not support range"");case""SERVER_BAD_CONTENT"":return(0,le.Z)(""The server could not find the file"");case""SERVER_UNAUTHORIZED"":return(0,le.Z)(""Unauthorized"");case""SERVER_CERT_PROBLEM"":return(0,le.Z)(""Certificate problem"");case""SERVER_FORBIDDEN"":return(0,le.Z)(""Forbidden"");case""SERVER_UNREACHABLE"":return(0,le.Z)(""Unreachable"");case""SERVER_CONTENT_LENGTH_MISMATCH"":return(0,le.Z)(""Content length mismatch"");case""SERVER_CROSS_ORIGIN_REDIRECT"":return(0,le.Z)(""Cross origin redirect"");case""USER_CANCELED"":return(0,le.Z)(""Cancelled"");case""USER_SHUTDOWN"":return(0,le.Z)(""Shutdown"");case""CRASH"":return(0,le.Z)(""Crashed"")}return e}(e.error):"""",h=d?(0,le.Z)(""$1/$2 - stopped"",[o,a]):(0,le.Z)(""$3, $1/$2"",[o,a,l]);return Kp(""div"",{className:""DownloadItem-FileSize""},void 0,r&&`${r}, `,e.state===Yp?a:e.error?c:h)/* Customized by Ben */}";

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED, true);
    }

    [Fact]
    public void navigateToSubdomainParts() {
        string actual = tweak.navigateToSubdomainParts(ORIGINAL_BUNDLE_TEXT);

        string expected =
            @"&&this.props.subdomain.split(""."").map((part, index, whole) => OC(""span"", { className: ""UrlFragment--Lowlight UrlFragment-HostFragment-Subdomain"", onClick: e => { e.stopPropagation(); this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : """") + whole.slice(index).join(""."") + ""."" + this.props.basedomain + ""."" + this.props.tld + (this.props.port ? `:${this.props.port}` : """")); }}, undefined, part, ""."")) /* Customized by Ben */,";
        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, expected);

        expected =
            @"OC(""span"",{className:""UrlFragment--Highlight UrlFragment-HostFragment-Basedomain"",onClick: e => { e.stopPropagation(); this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : """") + this.props.basedomain + ""."" + this.props.tld + (this.props.port ? `:${this.props.port}` : """")); } /* Customized by Ben */},";
        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, expected);

        expected =
            @"OC(""span"",{className:""UrlFragment--Highlight UrlFragment-HostFragment-TLD"",onClick: e => { e.stopPropagation(); this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : """") + this.props.basedomain + ""."" + this.props.tld + (this.props.port ? `:${this.props.port}` : """")); } /* Customized by Ben */},";
        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, expected);

        expected =
            @"OC(""span"",{className:""UrlFragment--Lowlight UrlFragment-HostFragment-Port"",onClick: e => { e.stopPropagation(); this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : """") + this.props.basedomain + ""."" + this.props.tld + (this.props.port ? `:${this.props.port}` : """")); } /* Customized by Ben */},";
        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, expected);
    }

    [Fact]
    public void allowMovingMailBetweenAnyFolders() {
        string actual = tweak.allowMovingMailBetweenAnyFolders(ORIGINAL_BUNDLE_TEXT);

        const string EXPECTED =
            @".getAccounts(),r=[];return s.forEach((([e,n])=>{const i=m.Z.getFolderName(e,n);const typesOrdered = [""Inbox"", ""Drafts"", ""Sent"", ""Archive"", ""Trash"", ""Junk"", ""Other""];const accountFolders = N.Z.getFolders()[e];let o=[];for(const t of te)o.push(...N.Z.getPathsByType(e,t));if(o=o.filter(x => accountFolders[x].subscribed).filter((e=>e!==n)),o.length>0){const l=o.map((i=>{const s=m.Z.getFolderName(e,i);return{folder: accountFolders[i],handler:()=>x(t,e,n,e,i),label:(0,Y.e)(s),labelEnglish:s}})),d=[];for(const i in a)if(!s.find((e=>e[0]===i))){const s=a[i].folders;for(const o in s)for(const a of s[o]){const s=a.path;if(te.includes(o)){const o=m.Z.getFolderName(i,s);d.push({handler:()=>x(t,e,n,i,s),label:`${i}/${(0,Y.e)(o)}`,labelEnglish:`${i}/${o}`})}}}const c=l.concat(d);r.push(...c.sort((entryA, entryB) => (entryA.folder.type === entryB.folder.type) ? entryA.label.localeCompare(entryB.label) : typesOrdered.indexOf(entryA.folder.type) - typesOrdered.indexOf(entryB.folder.type)))/* Customized by Ben */}})),r};getCustomLabelsMenu=";

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED, true);
    }

    [Fact]
    public void disableAutoHeightForImagesInMailWithHeight() {
        string actual = tweak.disableAutoHeightForImagesInMailWithHeightAttribute(ORIGINAL_BUNDLE_TEXT);

        const string EXPECTED =
            @".createStyleRoot(this.props.style)}\n    html {\n      overflow-y: auto;\n    }\n    body {\n      ${e?"""":""white-space: pre-wrap;""}\n      ${e?"""":""overflow-wrap: break-word;""}\n      color: ${e?""black"":""var(--colorFgIntense)""};\n      background-color: ${e?""white"":""var(--colorBgIntense)""};\n      font-family: ${e?""sans-serif"":""monospace""};\n      margin: 24px;\n      line-height: 1.3;\n      height: auto !important;\n      min-height: calc(100% - 48px);\n    }\n    ${e?"""":""a { color: var(--colorHighlightBg); }""}\n    img {\n      display: inline-block;\n      vertical-align: top;\n      max-width: 100%;\n      \n    }\n    img:not([height]) { height: auto; } /* Customized by Ben */</style>`";

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
    }

}