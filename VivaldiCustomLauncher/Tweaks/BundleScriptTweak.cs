using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#nullable enable

namespace VivaldiCustomLauncher.Tweaks {

    public class BundleScriptTweak: AbstractScriptTweak {

        private const string TWEAK_TYPE = nameof(BundleScriptTweak);

        /// <exception cref="TweakException"></exception>
        protected internal override Task<string?> editFile(string bundleContents) => Task.Run((Func<Task<string?>>) (async () => {
            string newBundleContents = bundleContents;
            newBundleContents = increaseMaximumTabWidth(newBundleContents);
            newBundleContents = removeExtraSpacingFromTabBarRightSide(newBundleContents);
            newBundleContents = formatDownloadProgress(newBundleContents);
            newBundleContents = await closeTabOnBackGestureIfNoTabHistory(newBundleContents);
            newBundleContents = navigateToSubdomainParts(newBundleContents);
            newBundleContents = hideMailPanelHeaders(newBundleContents);
            newBundleContents = allowMovingMailBetweenAnyFolders(newBundleContents);
            newBundleContents = formatPhoneNumbers(newBundleContents);
            newBundleContents = formatCalendarAgendaDates(newBundleContents);
            return newBundleContents;
        }));

        /* Make Back also close the tab if the page can't go back
         * Secret code sources:
         * - Se.a.back(): from the original action
         * - g.a.getActivePage(): copy invocation of getActivePage() in COMMAND_CLONE_TAB
         * - a(93).a.getNavigationInfo(): find an invocation of _.a.getNavigationInfo() and find out how _ is declared
         * - p.a.close(): action of COMMAND_CLOSE_TAB
         */
        /// <exception cref="TweakException">if the tweak can't be applied</exception>
        internal virtual async Task<string> closeTabOnBackGestureIfNoTabHistory(string bundleContents) {
            const string METHOD_NAME = nameof(closeTabOnBackGestureIfNoTabHistory);

            Task<(string webpackInjector, int dependencyId, string intermediateVariable)?> navigationInfoMatchTask = Task.Run(
                (Func<(string webpackInjector, int dependencyId, string intermediateVariable)?>) (() => {
                    Match  getNavigationInfoMatch       = Regex.Match(bundleContents, @"\b(?<dependencyVariable>[\w$]{1,2})\.(?<intermediateVariable>[\w$]{1,2})\.getNavigationInfo\(");
                    string dependencyVariable           = getNavigationInfoMatch.Groups["dependencyVariable"].Value;
                    string intermediateVariable         = getNavigationInfoMatch.Groups["intermediateVariable"].Value;
                    int    getNavigationInfoMatchOffset = getNavigationInfoMatch.Index;

                    Regex  dependencyDeclarationPattern = new($@"\b{Regex.Escape(dependencyVariable)}=(?<webpackInjector>[\w$]{{1,2}})\((?<dependencyId>\d+)\)", RegexOptions.RightToLeft);
                    Match  dependencyDeclarationMatch   = dependencyDeclarationPattern.Match(bundleContents, getNavigationInfoMatchOffset);
                    string webpackInjector              = dependencyDeclarationMatch.Groups["webpackInjector"].Value;
                    int?   dependencyId                 = int.TryParse(dependencyDeclarationMatch.Groups["dependencyId"].Value, out int id) ? id : null;

                    return getNavigationInfoMatch.Success && dependencyDeclarationMatch.Success && dependencyId is not null
                        ? (webpackInjector, (int) dependencyId, intermediateVariable)
                        : null;
                }));

            Task<(string dependencyVariable, string intermediateVariable)?> closeMatchTask = Task.Run((Func<(string dependencyVariable, string intermediateVariable)?>) (() => {
                Match match = Regex.Match(bundleContents,
                    @"{name:""COMMAND_CLOSE_TAB"",action:(?<eventVariable>[\w$]{1,2})=>(?<dependencyVariable>[\w$]{1,2})\.(?<intermediateVariable>[\w$]{1,2})\.close\(\k<eventVariable>\.windowId\),");
                string dependencyVariable   = match.Groups["dependencyVariable"].Value;
                string intermediateVariable = match.Groups["intermediateVariable"].Value;
                return match.Success ? (dependencyVariable, intermediateVariable) : null;
            }));

            (string webpackInjector, int dependencyId, string intermediateVariable) navigationInfo = await navigationInfoMatchTask ??
                throw new TweakException("Failed to find dependency ID for navigation info (the webpack ID of the object you call .a.getNavigationInfo() on)", TWEAK_TYPE, METHOD_NAME);
            (string dependencyVariable, string intermediateVariable) closer = await closeMatchTask ??
                throw new TweakException("Failed to find dependency name for close method (the variable you call .a.close() on)", TWEAK_TYPE, METHOD_NAME);

            bool bundleWasReplaced = false;
            string replacedBundle = Regex.Replace(bundleContents,
                @"(?<prefix>{name:""COMMAND_PAGE_BACK"",action:(?<eventVariable>[\w$]{1,2})=>{const (?<activePage>[\w$]{1,2})=(?:[\w$]{1,2}\.)+getActivePage\(.*?\);)(?<goBack>.{1,100})(?=\},)",
                match => {
                    bundleWasReplaced = true;
                    return match.Groups["prefix"].Value +
                        CUSTOMIZED_COMMENT +
                        $"const navigationInfo = {match.Groups["activePage"].Value} && {navigationInfo.webpackInjector}({navigationInfo.dependencyId}).{navigationInfo.intermediateVariable}.getNavigationInfo({match.Groups["activePage"].Value}.id);" +
                        "if(!navigationInfo || navigationInfo.canGoBack){" +
                        match.Groups["goBack"].Value +
                        "} else {" +
                        $"{closer.dependencyVariable}.{closer.intermediateVariable}.close({match.Groups["eventVariable"].Value}.windowId);" +
                        "}";
                });

            if (bundleWasReplaced) {
                return replacedBundle;
            } else {
                throw new TweakException("Failed to find COMMAND_PAGE_BACK action to replace", TWEAK_TYPE, METHOD_NAME);
            }
        }

        /// <exception cref="TweakException">if the tweak can't be applied</exception>
        internal virtual string removeExtraSpacingFromTabBarRightSide(string bundleContents) => replaceOrThrow(bundleContents,
            new Regex(@"(?<prefix>\bgetStyles.{1,20}=>.{1,200}this\.props\.maxWidth)(?<suffix>,)"),
            match => match.Groups["prefix"].Value + "+62" + CUSTOMIZED_COMMENT + match.Groups["suffix"].Value,
            new TweakException("Failed to find maxWidth to add to", TWEAK_TYPE));

        /// <exception cref="TweakException">if the tweak can't be applied</exception>
        internal virtual string increaseMaximumTabWidth(string bundleContents) => replaceOrThrow(bundleContents,
            new Regex(@"(?<prefix>TabStrip\.jsx.{1,2000}\b[\w$]{1,2}=)180\b"),
            match => match.Groups["prefix"].Value + 4000 + CUSTOMIZED_COMMENT,
            new TweakException("Failed to find old max tab width to replace", TWEAK_TYPE));

        /// <exception cref="TweakException">if the tweak can't be applied</exception>
        internal virtual string formatDownloadProgress(string bundleContents) => replaceOrThrow(bundleContents,
            new Regex(
                @"\.fromNow\(\)(?<unmodified1>.{1,52}?)about a second(?<unmodified2>.{1,7000}?)\$1 of \$2 - stopped(?<unmodified3>.{1,48}?)\$1 of \$2 at \$3(?<unmodified4>.{1,1000}?),(?<sizeExpr>[^,]{1,80}?),(?<timeVar>\w+)&&` \(\$\{\k<timeVar>\}\)`\)"),
            match =>
                $".fromNow(true){CUSTOMIZED_COMMENT}{match.Groups["unmodified1"].Value}1 second{match.Groups["unmodified2"].Value}$1/$2 - stopped{match.Groups["unmodified3"].Value}$3, $1/$2{match.Groups["unmodified4"].Value},{match.Groups["timeVar"].Value}&&`${{{match.Groups["timeVar"].Value}}}, `,{match.Groups["sizeExpr"].Value}){CUSTOMIZED_COMMENT}",
            new TweakException("Failed to find old date manipulation to replace", TWEAK_TYPE));

        /// <exception cref="TweakException">if the tweak can't be applied</exception>
        internal virtual string navigateToSubdomainParts(string bundleContents) {
            Match moduleStartMatch = Regex.Match(bundleContents, @"\\\\HostFragment\.jsx.*?render\(\){");
            if (!moduleStartMatch.Success) {
                throw new TweakException("Failed to find render() method in HostFragment module", TWEAK_TYPE);
            }

            int searchStart = moduleStartMatch.Index + moduleStartMatch.Length; //start searching for invocations inside the render() method

            Regex subdomainPattern = new(@"&&(?<domCreator>[\w$]{1,2})\.createElement\(""span"",{className:""UrlFragment--Lowlight UrlFragment-HostFragment-Subdomain"",.*?\),");
            bundleContents = replaceOrThrow(bundleContents, subdomainPattern, match => "&&this.props.subdomain.split(\".\").map((part, index, whole) => " +
                    $"{match.Groups["domCreator"].Value}.createElement(\"span\", {{ " +
                    "className: \"UrlFragment--Lowlight UrlFragment-HostFragment-Subdomain\", " +
                    "onClick: e => { " +
                    "e.stopPropagation(); " +
                    "this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : \"\") + whole.slice(index).join(\".\") + \".\" + this.props.basedomain + \".\" + this.props.tld + (this.props.port ? `:${this.props.port}` : \"\")); " +
                    "}}, part, \".\")) " + CUSTOMIZED_COMMENT + ","
                , 1, searchStart, new TweakException("Failed to find subdomain", TWEAK_TYPE));

            const string BASEDOMAIN_ONCLICK = "onClick: e => { " +
                "e.stopPropagation(); " +
                "this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : \"\") + this.props.basedomain + \".\" + this.props.tld + (this.props.port ? `:${this.props.port}` : \"\")); " +
                "} " + CUSTOMIZED_COMMENT + ",";

            Regex basedomainPattern = new(@"className:""UrlFragment--Highlight UrlFragment-HostFragment-Basedomain"",");
            bundleContents = replaceOrThrow(bundleContents, basedomainPattern, match => match.Value + BASEDOMAIN_ONCLICK, 1, searchStart, new TweakException("Failed to find basedomain", TWEAK_TYPE));

            Regex tldPattern = new(@"className:""UrlFragment--Highlight UrlFragment-HostFragment-TLD"",");
            bundleContents = replaceOrThrow(bundleContents, tldPattern, match => match.Value + BASEDOMAIN_ONCLICK, 1, searchStart, new TweakException("Failed to find TLD", TWEAK_TYPE));

            Regex portPattern = new(@"className:""UrlFragment--Lowlight UrlFragment-HostFragment-Port"",");
            bundleContents = replaceOrThrow(bundleContents, portPattern, match => match.Value + BASEDOMAIN_ONCLICK, 1, searchStart, new TweakException("Failed to find port", TWEAK_TYPE));

            return bundleContents;
        }

        internal virtual string hideMailPanelHeaders(string bundleContents) {
            return Regex.Replace(bundleContents,
                @"(?<prefix>,[\w$]{1,2}=[\w$]{1,2}\?\[[\w$]{1,2}\]:[\w$]{1,2}\.concat\([\w$]{1,2}\)),", // ,u=c?[X3]:l.concat(o),
                match => $"{match.Groups["prefix"].Value}.slice(7){CUSTOMIZED_COMMENT},");
        }

        /// <summary>
        /// Allow special folders (e.g. Sent, Trash, Junk) to be mail move destinations, not just Inbox and Other, so that I can mark messages as Spam and Not Spam.
        /// Allow special folders (e.g. Sent, Trash, Junk) to be mail move sources, not just Inbox and Other, so that I can remove messages from the Junk E-mail heuristic folder.
        /// In the Move menu, put folders in the top-level menu, not a submenu, because the extra inputs are annoying.
        /// In the Move menu, only show subscribed folders to avoid cluttering the menu with worthless destinations.
        /// In the Move menu, alphabetize the folders, but group them by special use (Inbox first, then Drafts, then Sent, etc) to make visual scanning easier.
        /// This tweak relies on folder subscription statuses being exposed to the UI by the <see cref="BackgroundCommonBundleScriptTweak.exposeFolderSubscriptionStatus"/> tweak.
        /// </summary>
        /// <exception cref="TweakException">if the tweak can't be applied</exception>
        internal virtual string allowMovingMailBetweenAnyFolders(string bundleContents) => replaceOrThrow(bundleContents,
            new Regex(
                @"(?<prefix>getMoveToFolderMenu.{1,600}?\.isVirtualViewFolder\([\w$,]{1,20}\)&&)\[.{3,50}?\]\.includes\([\w$]{1,2}\)&&(?<pushFolderSnippet>.{1,200}?{)let (?<folderNamesVar>[\w$]{1,2})=(?<folderManagerVars>[\w$.]{1,5}?)\.getPathsByType\((?<smtpAddressVar>[\w$]{1,2}),.{1,100}?(?<originalFiltering>\k<folderNamesVar>=\k<folderNamesVar>\.filter.{1,100}?\.push\(){items:(?<handlerMap>.{1,100}?\){3}),\.{3}.{1,100}?\]\)\}\)"),
            match =>
                match.Groups["prefix"].Value +
                match.Groups["pushFolderSnippet"].Value +
                "const typesOrdered = [\"Inbox\", \"Drafts\", \"Sent\", \"Archive\", \"Trash\", \"Junk\", \"Other\"];" +
                $"let {match.Groups["folderNamesVar"].Value} = Object.entries({match.Groups["folderManagerVars"].Value}.getFolders()[{match.Groups["smtpAddressVar"].Value}])" +
                ".filter(([path, folder]) => folder.subscribed)" +
                ".sort(([pathA, folderA], [pathB, folderB]) => (folderA.type === folderB.type) ? pathA.localeCompare(pathB) : typesOrdered.indexOf(folderA.type) - typesOrdered.indexOf(folderB.type))" +
                ".map(([path, folder]) => path);" +
                match.Groups["originalFiltering"].Value +
                $"...{match.Groups["handlerMap"].Value})" +
                CUSTOMIZED_COMMENT,
            new TweakException("Failed to find getMoveToFolderMenu method", TWEAK_TYPE));

        /// <summary>
        /// Format phone numbers in the Contacts panel using the US E164 formats:
        /// <para>1 (234) 567-8901</para>
        /// <para>(234) 567-8901</para>
        /// <para>567-8901</para>
        /// </summary>
        /// <remarks>Formatting algorithm and unit tests: https://jsbin.com/sorohuv/edit?js,output </remarks>
        /// <exception cref="TweakException">if the tweak can't be applied</exception>
        internal virtual string formatPhoneNumbers(string bundleContents) => replaceOrThrow(bundleContents,
            // balanced capturing group pairs: https://www.regular-expressions.info/balancing.html
            new Regex(@"(?<=[""']addSpaces['""],)(?>(?>(?'open'\()[^()]*)+(?>(?'-open'\))[^()]*)+)+(?(open)(?!))"),
            _ => "raw => {" +
                "const digits = raw.replace(/[^0-9a-z]/ig, '');" +
                "switch (digits.length){" +
                "case 7:" +
                "  return digits.substr(0,3) + '-' + digits.substr(3,4);" +
                "case 10:" +
                "  return '(' + digits.substr(0,3) + ') ' + digits.substr(3,3) + '-' + digits.substr(6,4);" +
                "case 11:" +
                "  return digits[0] + ' (' + digits.substr(1,3) + ') ' + digits.substr(4,3) + '-' + digits.substr(7,4);" +
                "default:" +
                "  return digits;" +
                "}" +
                "}"
                + CUSTOMIZED_COMMENT,
            new TweakException("Failed to find addSpaces function", TWEAK_TYPE));

        /// <summary>
        /// <para>Format dates in calendar agenda view to include the day of the week.</para>
        /// <para>Before: Feb 13, 2022</para>
        /// <para>After:  Sun, Feb 13, 2022</para>
        /// <para>Sadly, the new format is no longer localized, but there is no localized format which produces this output in Moment.js</para>
        /// <para>Moment.js formatting documentation: https://momentjs.com/docs/#/displaying/format/ </para>
        /// </summary>
        /// <param name="bundleContents"></param>
        /// <returns></returns>
        internal virtual string formatCalendarAgendaDates(string bundleContents) => replaceOrThrow(bundleContents,
            new Regex(@"(?<prefix>['""]cal-tasks-row-date['""].{1,200}?\.format\()['""]ll['""](?=\))"),
            match => match.Groups["prefix"].Value +
                @"""ddd, MMM D, YYYY""" +
                CUSTOMIZED_COMMENT,
            new TweakException("Failed to find localized Moment formatting call", TWEAK_TYPE));

    }

}