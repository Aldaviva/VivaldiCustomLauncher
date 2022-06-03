#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.Core.Internal;
using FakeItEasy;
using Tests.Assertions;
using VivaldiCustomLauncher.Tweaks;
using Xunit;
using Xunit.Sdk;

namespace Tests;

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

        const string EXPECTED = @",Hie=4000/* Customized by Ben */,";

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
            @"{name:""COMMAND_PAGE_BACK"",action:e=>{const t=N.Z.getActivePage(e.windowId);/* Customized by Ben */const navigationInfo = t && n(1877).Z.getNavigationInfo(t.id);if(!navigationInfo || navigationInfo.canGoBack){t&&D.Z.back(e.windowId,t.id)} else {b.Z.close(e.windowId);}},";

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
    }

    [Fact]
    public void formatDownloadProgress() {
        string actual = tweak.formatDownloadProgress(ORIGINAL_BUNDLE_TEXT);

        const string EXPECTED =
            @"return e&&t>n?g()(t).fromNow(true)/* Customized by Ben */:e&&t<=n?(0,he.Z)(""1 second""):""""}(s,n,t),c=(0,he.Z)(""$1/s"",[Zr(e.currentSpeed)]),u=e.paused||e.state===Cf,m=e.error?function(e){switch(e){case""FILE_NO_SPACE"":return(0,he.Z)(""Disk is full"");case""FILE_TOO_LARGE"":return(0,he.Z)(""File too large"");case""FILE_FAILED"":return(0,he.Z)(""Download error"");case""FILE_ACCESS_DENIED"":return(0,he.Z)(""Insufficient permissions"");case""FILE_NAME_TOO_LONG"":return(0,he.Z)(""Path too long"");case""FILE_VIRUS_INFECTED"":return(0,he.Z)(""Virus detected"");case""FILE_TRANSIENT_ERROR"":return(0,he.Z)(""System busy"");case""FILE_BLOCKED"":return(0,he.Z)(""Blocked"");case""FILE_SECURITY_CHECK_FAILED"":return(0,he.Z)(""Virus scan failed"");case""FILE_TOO_SHORT"":return(0,he.Z)(""File truncated"");case""FILE_SAME_AS_SOURCE"":return(0,he.Z)(""Already downloaded"");case""FILE_HASH_MISMATCH"":return(0,he.Z)(""Hash mismatch"");case""NETWORK_FAILED"":return(0,he.Z)(""Network"");case""NETWORK_TIMEOUT"":return(0,he.Z)(""Timeout"");case""NETWORK_DISCONNECTED"":return(0,he.Z)(""Disconnected"");case""NETWORK_SERVER_DOWN"":return(0,he.Z)(""Server unavailable"");case""NETWORK_INVALID_REQUEST"":return(0,he.Z)(""Invalid network request"");case""SERVER_FAILED"":return(0,he.Z)(""Server failed"");case""SERVER_NO_RANGE"":return(0,he.Z)(""Server does not support range"");case""SERVER_BAD_CONTENT"":return(0,he.Z)(""The server could not find the file"");case""SERVER_UNAUTHORIZED"":return(0,he.Z)(""Unauthorized"");case""SERVER_CERT_PROBLEM"":return(0,he.Z)(""Certificate problem"");case""SERVER_FORBIDDEN"":return(0,he.Z)(""Forbidden"");case""SERVER_UNREACHABLE"":return(0,he.Z)(""Unreachable"");case""SERVER_CONTENT_LENGTH_MISMATCH"":return(0,he.Z)(""Content length mismatch"");case""SERVER_CROSS_ORIGIN_REDIRECT"":return(0,he.Z)(""Cross origin redirect"");case""USER_CANCELED"":return(0,he.Z)(""Cancelled"");case""USER_SHUTDOWN"":return(0,he.Z)(""Shutdown"");case""CRASH"":return(0,he.Z)(""Crashed"")}return e}(e.error):"""",d=u?(0,he.Z)(""$1/$2 - stopped"",[a,o]):(0,he.Z)(""$3, $1/$2"",[a,o,c]);return i.createElement(""div"",{className:""DownloadItem-FileSize"",__source:{fileName:""D:\\builder\\workers\\ow64\\build\\vivaldi\\vivapp\\src\\components\\downloads\\DownloadPanel\\DownloadSize.jsx"",lineNumber:159,columnNumber:5}},l&&`${l},";

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

    [Fact(Skip = "unused")]
    [Obsolete]
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
            @"i.createElement(""div"",{className:""cal-tasks-row-date"",__source:{fileName:l$,lineNumber:355,columnNumber:13}},l?.format(""ddd, MMM D, YYYY""/* Customized by Ben */)),";

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
    }

    [Fact]
    public void allowMovingMailBetweenAnyFolders() {
        string actual = tweak.allowMovingMailBetweenAnyFolders(ORIGINAL_BUNDLE_TEXT);

        const string EXPECTED =
            @"getMoveToFolderMenu=e=>{const t=ie(e),n=t.map((e=>{const t=[];return e.sources.forEach(((e,n)=>{e.internal||e.deleted||t.push(n)})),t}));if(0===n.length)return[];const s=n.reduce(((e,t)=>e.filter((e=>t.includes(e))))),i=[];if(o.C.getByIds(s).forEach((e=>{const{accountId:t,path:n}=e,s=N.Z.getFolderType(t,n);m.Z.isMailAccount(t)&&te.includes(s)&&i.push([t,n])})),0===i.length){return[{...1===t.length?(0,X.Z)(""Can not move from this folder.""):(0,X.Z)(""No common folder. Can not move this selection.""),enabled:!1,mnemonic:!1}]}const a=m.Z.getAccounts(),r=[];return i.forEach((([e,n])=>{const s=m.Z.getFolderName(e,n);const typesOrdered = [""Inbox"", ""Drafts"", ""Sent"", ""Archive"", ""Trash"", ""Junk"", ""Other""];const accountFolders = N.Z.getFolders()[e];let o=[];for(const t of te)o.push(...N.Z.getPathsByType(e,t));if(o=o.filter(x => accountFolders[x].subscribed).filter((e=>e!==n)),o.length>0){const l=o.map((s=>{const i=m.Z.getFolderName(e,s);return{folder: accountFolders[s],handler:()=>x(t,e,n,e,s),label:(0,Y.e)(i),labelEnglish:i}})),d=[];for(const s in a)if(!i.find((e=>e[0]===s))){const i=a[s].folders;for(const o in i)for(const a of i[o]){const i=a.path;if(te.includes(o)){const o=m.Z.getFolderName(s,i);d.push({handler:()=>x(t,e,n,s,i),label:`${s}/${(0,Y.e)(o)}`,labelEnglish:`${s}/${o}`})}}}const c=l.concat(d);r.push(...c.sort((entryA, entryB) => (entryA.folder.type === entryB.folder.type) ? entryA.label.localeCompare(entryB.label) : typesOrdered.indexOf(entryA.folder.type) - typesOrdered.indexOf(entryB.folder.type)))/* Customized by Ben */}})),r};getCustomLabelsMenu=";

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
    }

    [Fact]
    public void disableAutoHeightForImagesInMailWithHeight() {
        string actual = tweak.disableAutoHeightForImagesInMailWithHeightAttribute(ORIGINAL_BUNDLE_TEXT);

        const string EXPECTED =
            @",uI(this,""getDefaultStyle"",(e=>`<style type=""text/css"">\n    ${Ay.Z.createStyleRoot(this.props.style)}\n    html {\n      overflow-y: auto;\n    }\n    body {\n      ${e?"""":""white-space: pre-wrap;""}\n      color: ${e?""black"":""var(--colorFgIntense)""};\n      background-color: ${e?""white"":""var(--colorBgIntense)""};\n      font-family: ${e?""sans-serif"":""monospace""};\n      margin: 24px;\n      line-height: 1.3;\n      height: auto !important;\n      min-height: calc(100% - 48px);\n    }\n    ${e?"""":""a { color: var(--colorHighlightBg); }""}\n    img {\n      display: inline-block;\n      vertical-align: top;\n      max-width: 100%;\n      \n    }\n    img:not([height]) { height: auto; } /* Customized by Ben */</style>`)),";

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
    }

}