﻿using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#nullable enable

namespace VivaldiCustomLauncher.Tweaks {

    public class BundleScriptTweak: AbstractScriptTweak {

        /// <exception cref="TweakException"></exception>
        protected override Task<string?> editFile(string bundleContents) => Task.Run((Func<Task<string?>>) (async () => {
            string newBundleContents = increaseMaximumTabWidth(bundleContents);
            newBundleContents = removeExtraSpacingFromTabBarRightSide(newBundleContents);
            newBundleContents = formatDownloadProgress(newBundleContents);
            newBundleContents = await closeTabOnBackGestureIfNoTabHistory(newBundleContents);
            newBundleContents = navigateToSubdomainParts(newBundleContents);
            newBundleContents = hideMailPanelHeaders(newBundleContents);
            newBundleContents = allowMovingMailBetweenAnyFolders(newBundleContents);
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
        internal async Task<string> closeTabOnBackGestureIfNoTabHistory(string bundleContents) {
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

            Task<string?> getActivePageMatchTask = Task.Run(() => emptyToNull(Regex.Match(bundleContents,
                    @"{name:""COMMAND_CLONE_TAB"",action:.*?(?<dependencyVariable>[\w$]{1,2}\.\w{1,2})\.unsafeGetActivePage\(\)")
                .Groups["dependencyVariable"].Value));

            Task<(string dependencyVariable, string intermediateVariable)?> closeMatchTask = Task.Run((Func<(string dependencyVariable, string intermediateVariable)?>) (() => {
                Match  match = Regex.Match(bundleContents, @"{name:""COMMAND_CLOSE_TAB"",action:\(\)=>(?<dependencyVariable>[\w$]{1,2})\.(?<intermediateVariable>[\w$]{1,2})\.close\(\),");
                string dependencyVariable = match.Groups["dependencyVariable"].Value;
                string intermediateVariable = match.Groups["intermediateVariable"].Value;
                return match.Success ? (dependencyVariable, intermediateVariable) : null;
            }));

            const string TYPE_NAME   = nameof(BundleScriptTweak);
            const string METHOD_NAME = nameof(closeTabOnBackGestureIfNoTabHistory);

            (string webpackInjector, int dependencyId, string intermediateVariable) navigationInfo = await navigationInfoMatchTask ??
                throw new TweakException("Failed to find dependency ID for navigation info (the webpack ID of the object you call .a.getNavigationInfo() on)", TYPE_NAME, METHOD_NAME);
            string getActivePageDependencyVariable = await getActivePageMatchTask ??
                throw new TweakException("Failed to find dependency name for active page (the variable you call .unsafeGetActivePage() on", TYPE_NAME, METHOD_NAME);
            (string dependencyVariable, string intermediateVariable) closer = await closeMatchTask ??
                throw new TweakException("Failed to find dependency name for close method (the variable you call .a.close() on)", TYPE_NAME, METHOD_NAME);

            bool bundleWasReplaced = false;
            string replacedBundle = Regex.Replace(bundleContents,
                @"(?<prefix>{name:""COMMAND_PAGE_BACK"",action:)(?<backDependencyVariable>[\w$]{1,2})\.(?<backIntermediateVariable>[\w$]{1,2})\.back(?<suffix>,)",
                match => {
                    bundleWasReplaced = true;
                    return match.Groups["prefix"].Value +
                        "() => { " +
                        $"const activePage = {getActivePageDependencyVariable}.getActivePage(), " +
                        $"navigationInfo = activePage && {navigationInfo.webpackInjector}({navigationInfo.dependencyId}).{navigationInfo.intermediateVariable}.getNavigationInfo(activePage.id); " +
                        "navigationInfo && navigationInfo.canGoBack" +
                        $" ? {match.Groups["backDependencyVariable"].Value}.{match.Groups["backIntermediateVariable"].Value}.back()" +
                        $" : {closer.dependencyVariable}.{closer.intermediateVariable}.close() " +
                        "} " +
                        CUSTOMIZED_COMMENT +
                        match.Groups["suffix"].Value;
                });

            if (bundleWasReplaced) {
                return replacedBundle;
            } else {
                throw new TweakException("Failed to find COMMAND_PAGE_BACK action to replace", TYPE_NAME, METHOD_NAME);
            }
        }

        internal string removeExtraSpacingFromTabBarRightSide(string bundleContents) {
            return Regex.Replace(bundleContents,
                @"(?<prefix>\bgetStyles.{1,20}=>.{1,200}this\.props\.maxWidth)(?<suffix>,)",
                match => match.Groups["prefix"].Value + "+62" + CUSTOMIZED_COMMENT + match.Groups["suffix"].Value);
        }

        internal string increaseMaximumTabWidth(string bundleContents) {
            return Regex.Replace(bundleContents,
                @"(?<prefix>\bmaxWidth=)(?<minTabWidth>180)(?<suffix>,)",
                match => match.Groups["prefix"].Value + 4000 + CUSTOMIZED_COMMENT + match.Groups["suffix"].Value);
        }

        internal string formatDownloadProgress(string bundleContents) {
            return Regex.Replace(bundleContents,
                @"\.fromNow\(\)(?<unmodified1>.{1,52}?)about a second(?<unmodified2>.{1,7000}?)\$1 of \$2 - stopped(?<unmodified3>.{1,48}?)\$1 of \$2 at \$3(?<unmodified4>.{1,1000}?),(?<sizeExpr>[^,]{1,80}?),(?<timeVar>\w+)&&` \(\$\{\k<timeVar>\}\)`\)",
                match =>
                    $".fromNow(true){CUSTOMIZED_COMMENT}{match.Groups["unmodified1"].Value}1 second{match.Groups["unmodified2"].Value}$1/$2 - stopped{match.Groups["unmodified3"].Value}$3, $1/$2{match.Groups["unmodified4"].Value},{match.Groups["timeVar"].Value}&&`${{{match.Groups["timeVar"].Value}}}, `,{match.Groups["sizeExpr"].Value}){CUSTOMIZED_COMMENT}");
        }

        internal string navigateToSubdomainParts(string bundleContents) {
            Match moduleStartMatch = Regex.Match(bundleContents, @"\\\\HostFragment\.jsx.*?render\(\){");
            if (!moduleStartMatch.Success) return bundleContents;
            int searchStart = moduleStartMatch.Index + moduleStartMatch.Length; //start searching for invocations inside the render() method

            Regex subdomainPattern = new(@"&&(?<domCreator>[\w$]{1,2})\.createElement\(""span"",{className:""UrlFragment--Lowlight UrlFragment-HostFragment-Subdomain"",.*?\),");
            bundleContents = subdomainPattern.Replace(bundleContents, match => "&&this.props.subdomain.split(\".\").map((part, index, whole) => " +
                    $"{match.Groups["domCreator"].Value}.createElement(\"span\", {{ " +
                    "className: \"UrlFragment--Lowlight UrlFragment-HostFragment-Subdomain\", " +
                    "onClick: e => { " +
                    "e.stopPropagation(); " +
                    "this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : \"\") + whole.slice(index).join(\".\") + \".\" + this.props.basedomain + \".\" + this.props.tld + (this.props.port ? `:${this.props.port}` : \"\")); " +
                    "}}, part, \".\")) " + CUSTOMIZED_COMMENT + ","
                , 1, searchStart);

            const string BASEDOMAIN_ONCLICK = "onClick: e => { " +
                "e.stopPropagation(); " +
                "this.props.onGoToPath((this.props.scheme ? `${this.props.scheme}://` : \"\") + this.props.basedomain + \".\" + this.props.tld + (this.props.port ? `:${this.props.port}` : \"\")); " +
                "} " + CUSTOMIZED_COMMENT + ",";

            Regex basedomainPattern = new(@"className:""UrlFragment--Highlight UrlFragment-HostFragment-Basedomain"",");
            bundleContents = basedomainPattern.Replace(bundleContents, match => match.Value + BASEDOMAIN_ONCLICK, 1, searchStart);

            Regex tldPattern = new(@"className:""UrlFragment--Highlight UrlFragment-HostFragment-TLD"",");
            bundleContents = tldPattern.Replace(bundleContents, match => match.Value + BASEDOMAIN_ONCLICK, 1, searchStart);

            Regex portPattern = new(@"className:""UrlFragment--Lowlight UrlFragment-HostFragment-Port"",");
            bundleContents = portPattern.Replace(bundleContents, match => match.Value + BASEDOMAIN_ONCLICK, 1, searchStart);

            return bundleContents;
        }

        internal string hideMailPanelHeaders(string bundleContents) {
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
        internal string allowMovingMailBetweenAnyFolders(string bundleContents) {
            return Regex.Replace(bundleContents,
                @"(?<prefix>getMoveToFolderMenu.{1,600}?\.isVirtualViewFolder\([\w$]{1,2}\)&&)\[.{3,50}?\]\.includes\([\w$]{1,2}\)&&(?<pushFolderSnippet>.{1,200}?{)let (?<folderNamesVar>[\w$]{1,2})=(?<folderManagerVars>[\w$.]{1,5}?)\.getPathsByType\((?<smtpAddressVar>[\w$]{1,2}),.{1,100}?(?<originalFiltering>\k<folderNamesVar>=\k<folderNamesVar>\.filter.{1,100}?\.push\(){items:(?<handlerMap>.{1,100}?\){3}),\.{3}.{1,100}?\]\)\}\)",
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
                    CUSTOMIZED_COMMENT);
        }

    }

}