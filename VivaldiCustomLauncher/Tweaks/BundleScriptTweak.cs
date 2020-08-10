using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

#nullable enable

namespace VivaldiCustomLauncher.Tweaks {

    public class BundleScriptTweak: Tweak<string, BaseTweakParams> {

        private const string CUSTOMIZED_COMMENT = @"/* Customized by Ben */";
        private static readonly char[] EXPECTED_HEADER = CUSTOMIZED_COMMENT.ToCharArray();

        public async Task<string?> readFileAndEditIfNecessary(BaseTweakParams tweakParams) {
            string bundleContents;
            using FileStream file = File.Open(tweakParams.filename, FileMode.Open, FileAccess.Read);

            using (var reader = new StreamReader(file, Encoding.UTF8, false, 4 * 1024, true)) {
                var buffer = new char[EXPECTED_HEADER.Length];
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
            newBundleContents = await closeTabOnBackGestureIfNoTabHistory(newBundleContents);
            return newBundleContents;
        }

        /* Make Back also close the tab if the page can't go back
         * Secret code sources:
         * - Se.a.back(): from the original action
         * - g.a.getActivePage(): copy invocation of getActivePage() in COMMAND_CLONE_TAB
         * - a(93).a.getNavigationInfo(): find an invocation of _.a.getNavigationInfo() and find out how _ is declared (backreferences to the rescue)
         * - p.a.close(): action of COMMAND_CLOSE_TAB
         */
        internal async Task<string> closeTabOnBackGestureIfNoTabHistory(string bundleContents) {
            Task<int?> navigationInfoMatchTask = Task.Run(() => int.TryParse(Regex.Match(bundleContents,
                    @"(?<dependencyVariable>[\w$]{1,3})=a\((?<dependencyId>\d+)\).*?\b\k<dependencyVariable>\.a\.getNavigationInfo\(")
                .Groups["dependencyId"].Value, out int id) ? id : (int?) null);

            Task<string?> getActivePageMatchTask = Task.Run(() => emptyToNull(Regex.Match(bundleContents,
                    @"{name:""COMMAND_CLONE_TAB"",action:.*?(?<dependencyVariable>[\w$]{1,2})\.a\.getActivePage\(\)")
                .Groups["dependencyVariable"].Value));

            Task<string?> closeMatchTask = Task.Run(() => emptyToNull(Regex.Match(bundleContents,
                    @"{name:""COMMAND_CLOSE_TAB"",action:(?<dependencyVariable>[\w$]{1,2})\.a\.close,")
                .Groups["dependencyVariable"].Value));

            int? navigationInfoDependencyId = await navigationInfoMatchTask;
            string? getActivePageDependencyVariable = await getActivePageMatchTask;
            string? closeDependencyVariable = await closeMatchTask;

            if (navigationInfoDependencyId != null && getActivePageDependencyVariable != null && closeDependencyVariable != null) {
                return Regex.Replace(bundleContents,
                    @"(?<prefix>{name:""COMMAND_PAGE_BACK"",action:)(?<backDependencyVariable>[\w$]{1,2})\.a\.back(?<suffix>,)",
                    match => match.Groups["prefix"].Value +
                        "()=>{" +
                        $"const c={getActivePageDependencyVariable}.a.getActivePage()," +
                        $"e=c&&a({navigationInfoDependencyId}).a.getNavigationInfo(c.id);" +
                        "e&&e.canGoBack" +
                        $"?{match.Groups["backDependencyVariable"].Value}.a.back()" +
                        $":{closeDependencyVariable}.a.close()" +
                        "}" +
                        CUSTOMIZED_COMMENT +
                        match.Groups["suffix"].Value);
            } else {
                return bundleContents;
            }
        }

        internal string removeExtraSpacingFromTabBarRightSide(string bundleContents) {
            return Regex.Replace(bundleContents,
                @"(?<prefix>\bgetStyle.{1,20}e=>.{1,200}this\.props\.maxWidth)(?<suffix>,)",
                match => match.Groups["prefix"].Value + "+62" + CUSTOMIZED_COMMENT + match.Groups["suffix"].Value);
        }

        internal string increaseMaximumTabWidth(string bundleContents) {
            return Regex.Replace(bundleContents,
                @"(?<prefix>\bmaxWidth=)(?<minTabWidth>180)(?<suffix>,)",
                match => match.Groups["prefix"].Value + 4000 + CUSTOMIZED_COMMENT + match.Groups["suffix"].Value);
        }

        public async Task saveFile(string fileContents, BaseTweakParams tweakParams) {
            using FileStream file = File.Open(tweakParams.filename, FileMode.Open, FileAccess.ReadWrite);
            using var writer = new StreamWriter(file, Encoding.UTF8);
            file.Seek(0, SeekOrigin.Begin);
            await writer.WriteAsync(EXPECTED_HEADER);
            await writer.WriteAsync(fileContents);
            await writer.FlushAsync();
        }

        private static string? emptyToNull(string input) => string.IsNullOrEmpty(input) ? null : input;

    }

}