#nullable enable

using Castle.Core.Internal;
using FakeItEasy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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
            @"getTabStripWidth=()=>{const e=this.props.prefValues[G.kTabsShowSyncedTabsButton];if(""row""===this.props.direction){const t=Number(e)+Number(this.props.showTrashCan);return t>0?this.props.maxWidth+71/* Customized by Ben */-bpe*t:this.props.maxWidth+71/* Customized by Ben */-8}return this.props.maxWidth";

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
            @"=function(e,t,s){return e&&t>s?o()(t).fromNow(true)/* Customized by Ben */:e&&t<=s?(0,n.Z)(""1 second""):""""}(a,s,t),f=(0,n.Z)(""$1/s"",[(0,i.Z)(e.currentSpeed)]),v=e.paused||e.state===c,b=e.error?h(e.error):"""",C=v?(0,n.Z)(""$1/$2 - stopped"",[p,g]):(0,n.Z)(""$3, $1/$2"",[p,g,f]);return(0,r.jsxs)(""div"",{className:""DownloadItem-FileSize"",children:[m&&`${m}, `,e.state===d?g:e.error?b:C]})/* Customized by Ben */}},";

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
    public void expandDomainsWithHttps() {
        string actual = tweak.expandDomainsWithHttps(ORIGINAL_BUNDLE_TEXT);

        const string EXPECTED = @"return void await this._urlFieldGo(""https://"" /* Customized by Ben */ + (0,";

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
    }

    [Fact]
    public void hideNoisyStatusMessages() {
        string actual = tweak.hideNoisyStatusMessages(ORIGINAL_BUNDLE_TEXT);

        const string EXPECTED =
            @";case""STATUS_SET_STATUS"":return(0,Fd.c)(e,t.windowId,(e=>{e.status=[""Finished indexing - "",""Finished prefetching - "",""Checking calendar ""].some(prefix=>t.status.startsWith(prefix))?"""":t.status/* Customized by Ben */})";

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
    }

    [Fact]
    public void calculateDataSizesInBase1024() {
        string actual = tweak.calculateDataSizesInBase1024(ORIGINAL_BUNDLE_TEXT);

        const string EXPECTED =
            @";const mh=[(0,nt.Z)(""B""),(0,nt.Z)(""kB""),(0,nt.Z)(""MB""),(0,nt.Z)(""GB""),(0,nt.Z)(""TB""),(0,nt.Z)(""PB""),(0,nt.Z)(""EB""),(0,nt.Z)(""ZB""),(0,nt.Z)(""YB"")],vh=(0,nt.Z)(""bytes"");function fh(e,t,n=!0,i=true/* Customized by Ben */){if(isNaN(e))return e?e.toString():"""";if(e<=0)return""0 ""+(n?vh:mh[0]);const s=i?1024:1e3,";

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
    }

    [Fact]
    public void autoShowImagesInNonSpamEmails() {
        string actual = tweak.autoShowImagesInNonSpamEmails(ORIGINAL_BUNDLE_TEXT);

        const string EXPECTED =
            @",blockHTTPLeaks:n&&e.listEntry.subject.startsWith('Spam: ')/* Customized by Ben */,bodyParts:""html""===";

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
    }

}