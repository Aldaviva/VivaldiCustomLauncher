#nullable enable

using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks;

public class BundleScriptTweak: BaseScriptTweak {

    private const string TWEAK_TYPE = nameof(BundleScriptTweak);

    /// <exception cref="TweakException"></exception>
    protected internal override Task<string> editFile(string bundleContents) => Task.Run(() => {
        string newBundleContents = bundleContents;
        newBundleContents = increaseMaximumTabWidth(newBundleContents);
        newBundleContents = formatDownloadProgress(newBundleContents);
        newBundleContents = closeTabOnBackGestureIfNoTabHistory(newBundleContents);
        newBundleContents = navigateToSubdomainParts(newBundleContents);
        newBundleContents = allowMovingMailBetweenAnyFolders(newBundleContents);
        newBundleContents = expandDomainsWithHttps(newBundleContents);
        newBundleContents = hideNoisyStatusMessages(newBundleContents);
        newBundleContents = calculateDataSizesInBase1024(newBundleContents);
        newBundleContents = autoShowImagesInNonSpamEmails(newBundleContents);
        return newBundleContents;
    });

    /*
     * You could also look at the value of eventVariable.origin if you want to only allow this behavior for mouse gestures and not keyboard shortcuts, for example. By default it will run for all origins.
     */
    /// <summary>Make Back also close the tab if the page can't go back</summary>
    /// <exception cref="TweakException">if the tweak can't be applied</exception>
    internal virtual string closeTabOnBackGestureIfNoTabHistory(string bundleContents) {
        const string METHOD_NAME = nameof(closeTabOnBackGestureIfNoTabHistory);

        Match commandCloseMatch = Regex.Match(bundleContents,
            """{name:"COMMAND_CLOSE_TAB",action:(?<eventVariable>[\w$]{1,2})=>(?<dependencyVariable>[\w$]{1,2})\.(?<intermediateVariable>[\w$]{1,2})\.close\(\k<eventVariable>\.windowId\),""");
        if (!commandCloseMatch.Success) {
            throw new TweakException("Failed to find dependency name for close method (the variable you call .a.close() on)", TWEAK_TYPE, METHOD_NAME);
        }

        bool bundleWasReplaced = false;
        string replacedBundle = Regex.Replace(bundleContents,
            """(?<prefix>{name:"COMMAND_PAGE_BACK",action:(?<eventVariable>[\w$]{1,2})=>{const (?<activePage>[\w$]{1,2})=(?:[\w$]{1,2}\.)+getActivePage\(.*?\);)(?<goBack>.{1,100})(?=\},)""",
            commandBackMatch => {
                bundleWasReplaced = true;
                return commandBackMatch.Groups["prefix"].Value +
                    CUSTOMIZED_COMMENT +
                    "if(Object.entries(document.getElementById(\"portals\")).find(prop => prop[0].startsWith(\"__reactContainer$\"))[1].child.child.child.stateNode.state.appElm.querySelector(\".webpageview.active webview\").canGoBack()){"
                    +
                    commandBackMatch.Groups["goBack"].Value +
                    "} else {" +
                    $"{commandCloseMatch.Groups["dependencyVariable"].Value}.{commandCloseMatch.Groups["intermediateVariable"].Value}.close({commandBackMatch.Groups["eventVariable"].Value}.windowId);"
                    +
                    "}";
            });

        if (bundleWasReplaced) {
            return replacedBundle;
        } else {
            throw new TweakException("Failed to find COMMAND_PAGE_BACK action to replace", TWEAK_TYPE, METHOD_NAME);
        }
    }

    /// <exception cref="TweakException">if the tweak can't be applied</exception>
    internal virtual string increaseMaximumTabWidth(string bundleContents) => replaceOrThrow(bundleContents,
        new Regex("(?<![-+!<>=*/%&^|?])=180(?=[;,].{0,1000}isDraggingPinnedTab)"),
        _ => "=5200" + CUSTOMIZED_COMMENT,
        new TweakException("Failed to find old max tab width to replace", TWEAK_TYPE));

    /// <exception cref="TweakException">if the tweak can't be applied</exception>
    internal virtual string formatDownloadProgress(string bundleContents) => replaceOrThrow(bundleContents,
        new Regex(
            @"\.fromNow\(\)(?<unmodified1>.{1,52}?)about a second(?<unmodified2>.{1,7000}?)\$1 of \$2 - stopped(?<unmodified3>.{1,48}?)\$1 of \$2 at \$3(?<unmodified4>.{1,1000}?)\[(?<sizeExpr>[^,]{1,80}?),(?<timeVar>\w+)&&` \(\$\{\k<timeVar>\}\)`\]\}\)"),
        match =>
            $".fromNow(true){CUSTOMIZED_COMMENT}{match.Groups["unmodified1"].Value}1 second{match.Groups["unmodified2"].Value}$1/$2 - stopped{match.Groups["unmodified3"].Value}$3, $1/$2{match.Groups["unmodified4"].Value}[{match.Groups["timeVar"].Value}&&`${{{match.Groups["timeVar"].Value}}}, `,{match.Groups["sizeExpr"].Value}]}}){CUSTOMIZED_COMMENT}",
        new TweakException("Failed to find old date manipulation to replace", TWEAK_TYPE));

    /// <exception cref="TweakException">if the tweak can't be applied</exception>
    internal virtual string navigateToSubdomainParts(string bundleContents) {
        Match moduleStartMatch = Regex.Match(bundleContents, @"\brender\(\){.{1,600}?UrlFragment--Lowlight");
        if (!moduleStartMatch.Success) {
            throw new TweakException("Failed to find render() method in HostFragment module", TWEAK_TYPE);
        }

        int searchStart = moduleStartMatch.Index; //start searching for invocations inside the render() method

        Regex subdomainPattern = new(@"&&(?<domCreator>\(0,[\w$\.]{1,8}\))\(""span"",{className:""UrlFragment--Lowlight UrlFragment-HostFragment-Subdomain"".*?\),");
        bundleContents = replaceOrThrow(bundleContents, subdomainPattern, match => "&&this.props.subdomain.split(\".\").map((part, index, whole) => " +
                $"{match.Groups["domCreator"].Value}(\"span\", {{ " +
                "className: \"UrlFragment--Lowlight UrlFragment-HostFragment-Subdomain\", " +
                "onClick: e => { e.stopPropagation(); this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : \"\") + whole.slice(index).join(\".\") + \".\" + this.props.basedomain + \".\" + this.props.tld + (this.props.port ? `:${this.props.port}` : \"\")); }," +
                "children: [part, \".\"] })) " + CUSTOMIZED_COMMENT + ","
            , 1, searchStart, new TweakException("Failed to find subdomain", TWEAK_TYPE));

        const string BASEDOMAIN_ONCLICK =
            $",onClick: e => {{ e.stopPropagation(); this.props.onGoToPath((this.props.scheme ? `${{this.props.scheme}}://` : \"\") + this.props.basedomain + \".\" + this.props.tld + (this.props.port ? `:${{this.props.port}}` : \"\")); }} {CUSTOMIZED_COMMENT}";

        Regex basedomainPattern = new(@"className:""UrlFragment--Highlight UrlFragment-HostFragment-Basedomain""");
        bundleContents = replaceOrThrow(bundleContents, basedomainPattern, match => match.Value + BASEDOMAIN_ONCLICK, 1, searchStart, new TweakException("Failed to find basedomain", TWEAK_TYPE));

        Regex tldPattern = new(@"className:""UrlFragment--Highlight UrlFragment-HostFragment-TLD""");
        bundleContents = replaceOrThrow(bundleContents, tldPattern, match => match.Value + BASEDOMAIN_ONCLICK, 1, searchStart, new TweakException("Failed to find TLD", TWEAK_TYPE));

        Regex portPattern = new(@"className:""UrlFragment--Lowlight UrlFragment-HostFragment-Port""");
        bundleContents = replaceOrThrow(bundleContents, portPattern, match => match.Value + BASEDOMAIN_ONCLICK, 1, searchStart, new TweakException("Failed to find port", TWEAK_TYPE));

        return bundleContents;
    }

    /// <summary>
    /// Allow special folders (e.g. Sent, Trash, Junk) to be mail move destinations, not just Inbox and Other, so that I can mark messages as Spam and Not Spam. (not needed in 5.3)
    /// Allow special folders (e.g. Sent, Trash, Junk) to be mail move sources, not just Inbox and Other, so that I can remove messages from the Junk E-mail heuristic folder. (not needed in 5.3)
    /// In the Move menu, put folders in the top-level menu, not a submenu, because the extra inputs are annoying.
    /// In the Move menu, only show subscribed folders to avoid cluttering the menu with worthless destinations.
    /// In the Move menu, alphabetize the folders, but group them by special use (Inbox first, then Drafts, then Sent, etc) to make visual scanning easier.
    /// </summary>
    /// <exception cref="TweakException">if the tweak can't be applied</exception>
    internal virtual string allowMovingMailBetweenAnyFolders(string bundleContents) {
        Match functionStartMatch = Regex.Match(bundleContents, "getMoveToFolderMenu=");
        if (!functionStartMatch.Success) {
            throw new TweakException("Failed to find getMoveToFolderMenu() function", TWEAK_TYPE);
        }

        int searchStart = functionStartMatch.Index + functionStartMatch.Length; //start searching for matches inside the getMoveToFolderMenu() method

        Match folderManagerVarMatch = new Regex(@"\b(?<folderManagerVar>[\w$.]{1,7}?)\.getPathsByType\(").Match(bundleContents, searchStart);
        if (!folderManagerVarMatch.Success) {
            throw new TweakException("Failed to find folder manager variable (on which to call getFolders())", TWEAK_TYPE);
        }

        string folderManagerVar = folderManagerVarMatch.Groups["folderManagerVar"].Value;

        bundleContents = replaceOrThrow(bundleContents, new Regex(@"\.getFolderName\((?<smtpAddressVar>[\w$]{1,3}),[\w$]{1,3}\);"),
            match => match.Value +
                "const typesOrdered = [\"Inbox\", \"Drafts\", \"Sent\", \"Archive\", \"Trash\", \"Junk\", \"Other\"];" +
                $"const accountFolders = {folderManagerVar}.getFolders()[{match.Groups["smtpAddressVar"].Value}];",
            1, searchStart, new TweakException("Failed to find account name and origin path variables", TWEAK_TYPE));

        bundleContents = replaceOrThrow(bundleContents, new Regex(@"(?<folderNamesVar>[\w$]{1,2})=\k<folderNamesVar>\.filter\("),
            match => match.Value + "x => accountFolders[x].subscribed).filter(",
            1, searchStart, new TweakException("Failed to insert filter", TWEAK_TYPE));

        bundleContents = replaceOrThrow(bundleContents, new Regex(@"\b(?<folderPathVar>[\w$]{1,3})\);return{"),
            match => match.Value + $"folder: accountFolders[{match.Groups["folderPathVar"].Value}],",
            1, searchStart, new TweakException("Failed to find menu item object", TWEAK_TYPE));

        bundleContents = replaceOrThrow(bundleContents, new Regex(@"\.push\({items:(?<allMenuItemsVar>[\w$]{1,3}).{1,200}?}\)"),
            match => $".push(...{match.Groups["allMenuItemsVar"].Value}.sort((entryA, entryB) => " +
                "(entryA.folder.type === entryB.folder.type) ? entryA.label.localeCompare(entryB.label) : typesOrdered.indexOf(entryA.folder.type) - typesOrdered.indexOf(entryB.folder.type)))" +
                CUSTOMIZED_COMMENT,
            1, searchStart, new TweakException("Failed to find final push", TWEAK_TYPE));

        return bundleContents;
    }

    /// <summary>
    /// By default, typing something like "google" in the address bar and pressing Ctrl+Enter will navigate to "google.com". Unfortunately, even when HTTPS-Only Mode is enabled, this expansion will initially navigate to "http://google.com" before HTTPS-Only Mode, HSTS, or server-side redirections kick in.
    /// Modify the function so that it would initially navigate to "https://google.com" instead.
    /// </summary>
    /// <exception cref="TweakException">if the tweak can't be applied</exception>
    internal virtual string expandDomainsWithHttps(string bundleContents) => replaceOrThrow(bundleContents,
        new Regex(@"(?<prefix>\.kAddressBarAutocompleteSuffixExpansionValue.{1,100}_urlFieldGo\()"),
        match => $"{match.Groups["prefix"].Value}\"https://\"{CUSTOMIZED_COMMENT}+",
        new TweakException("Failed to find handleSubmit function that reads kAddressBarAutocompleteSuffixExpansionEnabled and calls _urlFieldGo()", TWEAK_TYPE));

    /// <exception cref="TweakException">if the tweak can't be applied</exception>
    internal virtual string hideNoisyStatusMessages(string bundleContents) {
        // language=json
        const string PREFIXES_TO_BLOCK_JSON =
            """
            [
                "Finished indexing - ",
                "Finished prefetching - ",
                "Checking calendar ",
                "All downloaded messages are available for a full text search",
                "Running mail filters..."
            ]
            """;
        return replaceOrThrow(bundleContents,
            new Regex(@"(?<prefix>case""STATUS_SET_STATUS"":.{16,64}?\{[\w$]{1,3}\.status=)(?<actionVar>[\w$]{1,3})\.status(?=\})"),
            match =>
                $"{match.Groups["prefix"].Value}{PREFIXES_TO_BLOCK_JSON}.some(prefix=>{match.Groups["actionVar"].Value}.status.startsWith(prefix))?\"\":{match.Groups["actionVar"].Value}.status{CUSTOMIZED_COMMENT}",
            new TweakException("Could not find reduce function with a switch statement on actionType with a case for STATUS_SET_STATUS", TWEAK_TYPE));
    }

    /// <summary>
    /// Starting in version 6.2, Vivaldi formats data sizes in base-1000 (1000 B = 1 kB, 1000 kB = 1 MB).
    /// This is a feature, not a bug: https://forum.vivaldi.net/topic/92075/option-to-select-binary-or-decimal-units-for-file-sizes
    /// Revert this to the correct base of 1024 (1024 B = 1 kB, 1024 kB = 1 MB).
    /// </summary>
    /// <exception cref="TweakException">if the tweak can't be applied</exception>
    internal virtual string calculateDataSizesInBase1024(string bundleContents) => replaceOrThrow(bundleContents, new Regex(
            """(?<prefix>["']B["'].{1,22}?["']kB["'].{1,22}?["']MB["'].{1,22}?["']GB["'].{1,22}?["']TB["'].{1,22}?["']PB["'].{1,22}?["']EB["'].{1,22}?["']ZB["'].{1,22}?["']YB["'].{1,48}?\bfunction [\w$]{1,2}\([\w$]{1,2},[\w$]{1,2},[\w$]{1,2}=[^,]+,)(?<useBase1024Variable>[\w$]{1,2})=!1(?=\).{1,156}\2\?1024:1e3\b)"""),
        match => $"{match.Groups["prefix"].Value}{match.Groups["useBase1024Variable"].Value}=true{CUSTOMIZED_COMMENT}",
        new TweakException("Failed to find data size formatting function after B, kB, MB, GB, etc", TWEAK_TYPE));

    /// <summary>
    /// Unlike Gmail and Outlook, Vivaldi always hides images and other content in emails from senders not in your address book, so you always have to click the Load External Content button for each message (although it does remember per message). This is good for blocking tracking pixels, but my spam filter is good enough to keep malicious mails out of my Inbox, so this is just a waste of time for me with no added security or privacy.
    /// With this tweak, all emails will always show images, even if the sender is not in your address book, unless the message was marked as spam with the "Spam: " subject prefix (and it will be in your Junk E-mail folder in that case).
    /// </summary>
    /// <exception cref="TweakException">if the tweak can't be applied</exception>
    internal virtual string autoShowImagesInNonSpamEmails(string bundleContents) => replaceOrThrow(bundleContents, new Regex(
            """(?<prefix>{style:[\w$]{1,3},blockHTTPLeaks:[\w$]{1,3})(?=,.{0,236}?messageHeader:(?<emailObj>[\w$]{1,3})\.messageHeader,)"""),
        match => $"{match.Groups["prefix"].Value}&&{match.Groups["emailObj"].Value}.listEntry.subject.startsWith('Spam: '){CUSTOMIZED_COMMENT}",
        new TweakException("Failed to find email rendering method that determines the style, bodyParts, fromAddress, shouldWarnUserReplyTo, and other properties", TWEAK_TYPE));

}