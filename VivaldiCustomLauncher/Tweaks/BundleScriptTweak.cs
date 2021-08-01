using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#nullable enable

namespace VivaldiCustomLauncher.Tweaks {

    public class BundleScriptTweak: Tweak<string, BaseTweakParams> {

        private const           string CUSTOMIZED_COMMENT = @"/* Customized by Ben */";
        private static readonly char[] EXPECTED_HEADER    = CUSTOMIZED_COMMENT.ToCharArray();

        public async Task<string?> readFileAndEditIfNecessary(BaseTweakParams tweakParams) {
            string           bundleContents;
            using FileStream file = File.Open(tweakParams.filename, FileMode.Open, FileAccess.Read);

            using (StreamReader reader = new(file, Encoding.UTF8, false, 4 * 1024, true)) {
                char[] buffer = new char[EXPECTED_HEADER.Length];
                await reader.ReadAsync(buffer, 0, buffer.Length);

                if (EXPECTED_HEADER.SequenceEqual(buffer)) {
                    return null;
                }

                file.Seek(0, SeekOrigin.Begin);
                reader.DiscardBufferedData();
                bundleContents = await reader.ReadToEndAsync();
            }

            string newBundleContents = increaseMaximumTabWidth(bundleContents);
            newBundleContents = removeExtraSpacingFromTabBarRightSide(newBundleContents);
            newBundleContents = formatDownloadProgress(newBundleContents);
            newBundleContents = await closeTabOnBackGestureIfNoTabHistory(newBundleContents);
            newBundleContents = navigateToSubdomainParts(newBundleContents);
            return newBundleContents;
        }

        /* Make Back also close the tab if the page can't go back
         * Secret code sources:
         * - Se.a.back(): from the original action
         * - g.a.getActivePage(): copy invocation of getActivePage() in COMMAND_CLONE_TAB
         * - a(93).a.getNavigationInfo(): find an invocation of _.a.getNavigationInfo() and find out how _ is declared
         * - p.a.close(): action of COMMAND_CLOSE_TAB
         */
        internal async Task<string> closeTabOnBackGestureIfNoTabHistory(string bundleContents) {
            Task<int?> navigationInfoMatchTask = Task.Run(() => {
                Match  getNavigationInfoMatch       = Regex.Match(bundleContents, @"\b(?<dependencyVariable>[\w$]{1,2})\.a\.getNavigationInfo\(");
                string dependencyVariable           = getNavigationInfoMatch.Groups["dependencyVariable"].Value;
                int    getNavigationInfoMatchOffset = getNavigationInfoMatch.Index;

                Regex dependencyDeclarationPattern = new($@"\b{Regex.Escape(dependencyVariable)}=a\((?<dependencyId>\d+)\)", RegexOptions.RightToLeft);
                Match dependencyDeclarationMatch   = dependencyDeclarationPattern.Match(bundleContents, getNavigationInfoMatchOffset);

                return int.TryParse(dependencyDeclarationMatch.Groups["dependencyId"].Value, out int id) ? id : (int?) null;
            });

            Task<string?> getActivePageMatchTask = Task.Run(() => emptyToNull(Regex.Match(bundleContents,
                    @"{name:""COMMAND_CLONE_TAB"",action:.*?(?<dependencyVariable>[\w$]{1,2}\.\w{1,2})\.unsafeGetActivePage\(\)")
                .Groups["dependencyVariable"].Value));

            Task<string?> closeMatchTask = Task.Run(() => emptyToNull(Regex.Match(bundleContents,
                    @"{name:""COMMAND_CLOSE_TAB"",action:\(\)=>(?<dependencyVariable>[\w$]{1,2})\.a\.close\(\),")
                .Groups["dependencyVariable"].Value));

            int?    navigationInfoDependencyId      = await navigationInfoMatchTask;
            string? getActivePageDependencyVariable = await getActivePageMatchTask;
            string? closeDependencyVariable         = await closeMatchTask;

            if (navigationInfoDependencyId != null && getActivePageDependencyVariable != null && closeDependencyVariable != null) {
                return Regex.Replace(bundleContents,
                    @"(?<prefix>{name:""COMMAND_PAGE_BACK"",action:)(?<backDependencyVariable>[\w$]{1,2})\.a\.back(?<suffix>,)",
                    match => match.Groups["prefix"].Value +
                        "() => { " +
                        $"const activePage = {getActivePageDependencyVariable}.getActivePage(), " +
                        $"navigationInfo = activePage && a({navigationInfoDependencyId}).a.getNavigationInfo(activePage.id); " +
                        "navigationInfo && navigationInfo.canGoBack" +
                        $" ? {match.Groups["backDependencyVariable"].Value}.a.back()" +
                        $" : {closeDependencyVariable}.a.close() " +
                        "} " +
                        CUSTOMIZED_COMMENT +
                        match.Groups["suffix"].Value);
            } else {
                return bundleContents;
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
                @"\.fromNow\(\):(?<unmodified1>.{1,700}),(?<unmodified2>\w\.state===.{1,50})\(""\$1 of \$2 - stopped""(?<unmodified3>.{1,50})""\$1 of \$2 at \$3"",(?<unmodified4>.{1,50}),(?<timeVar>\w+)&&` \(\$\{\k<timeVar>\}\)`\)",
                match =>
                    $"{CUSTOMIZED_COMMENT}.fromNow(true):{match.Groups["unmodified1"].Value},{match.Groups["timeVar"].Value}&&`${{{match.Groups["timeVar"].Value}}}, `,{match.Groups["unmodified2"].Value}(\"$1/$2 - stopped\"{match.Groups["unmodified3"].Value}\"$3, $1/$2\",{match.Groups["unmodified4"].Value})");
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

        public async Task saveFile(string fileContents, BaseTweakParams tweakParams) {
            using FileStream   file   = File.Open(tweakParams.filename, FileMode.Open, FileAccess.ReadWrite);
            using StreamWriter writer = new(file, Encoding.UTF8);
            file.Seek(0, SeekOrigin.Begin);
            await writer.WriteAsync(EXPECTED_HEADER);
            await writer.WriteAsync(fileContents);
            await writer.FlushAsync();
        }

        private static string? emptyToNull(string input) => string.IsNullOrEmpty(input) ? null : input;

    }

}