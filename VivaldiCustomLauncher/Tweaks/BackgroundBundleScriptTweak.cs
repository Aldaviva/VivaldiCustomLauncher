#nullable enable

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks;

public class BackgroundBundleScriptTweak: BaseScriptTweak {

    private const string TWEAK_TYPE = nameof(BackgroundBundleScriptTweak);

    protected internal override Task<string> editFile(string bundleContents) => Task.Run(() => {
        string newBundleContents = bundleContents;
        newBundleContents = classifyJunkEmailAsNormalFolder(newBundleContents);
        return newBundleContents;
    });

    /// <summary>
    /// <para>Invalidate service worker script cache so that our changes to <c>background-bundle.js</c> take effect on the next launch.</para>
    /// <para>Fix for issue #4</para>
    /// </summary>
    public override async Task<string> readAndEditFile(BaseTweakParams tweakParams) {
        string newFileContentsToWrite = await base.readAndEditFile(tweakParams);

        DirectoryInfo serviceWorkerDirectory = new(Environment.ExpandEnvironmentVariables(
            @"%LOCALAPPDATA%\Vivaldi\User Data\Default\Storage\ext\mpognobbkildjkofajifpdfhcoklimli\def\Service Worker\"));
        string backupServiceWorkerDirectory = Path.Combine(serviceWorkerDirectory.Parent!.FullName, serviceWorkerDirectory.Name + "-old");

        try {
            Directory.Delete(backupServiceWorkerDirectory, true);
        } catch (DirectoryNotFoundException) {
            // An old backup already didn't exist, so we can continue with renaming the current directory to its backup name.
        }

        try {
            Directory.Move(serviceWorkerDirectory.FullName, backupServiceWorkerDirectory);
        } catch (DirectoryNotFoundException) {
            // The Service Worker directory didn't even exist in the first place, so we're already done. This can happen on a new Vivaldi installation.
        }

        return newFileContentsToWrite;
    }

    /// <summary>
    /// <para>Special case to use MDaemon's "Junk E-mail" folder as not a junk folder for Vivaldi. This is because it's a source of possible junk, not a sink of confirmed junk like Vivaldi assumes.
    /// MDaemon puts suspected junk in here, but you shouldn't put it in here yourself since training happens in "Public Folders/Spam", and clicking Mark Message As Spam in Vivaldi would simply unmark
    /// it as junk and move it to the Inbox, instead of preserving the junk flag and moving it to the confirmed Spam folder. Also, when a folder has the Junk special use flag, it tends to show all
    /// junk messages from all folders (like a search folder), instead of only the messages in the real folder.</para>
    ///
    /// <para>The name Junk E-mail comes from a mailbox folder that I create for all users in MDaemon. Suspected junk with the X-SPAM-FLAG header set to Yes (by scoring in the interval [5,12) in
    /// MDaemon's spam filter engine) is moved to these folders with per-account IMAP filtering rules that I also create for each user.</para>
    /// </summary>
    internal virtual string classifyJunkEmailAsNormalFolder(string bundleContents) => replaceOrThrow(bundleContents,
        new Regex(@"if\((?<folderVar>[\w$]{1,3})\.flags\)"),
        match => $"if({match.Groups["folderVar"].Value}.path === \"Junk E-mail\"){{ return false; }}{CUSTOMIZED_COMMENT}{match.Value}",
        new TweakException("Failed to find IMAP folder special use function", TWEAK_TYPE));

}