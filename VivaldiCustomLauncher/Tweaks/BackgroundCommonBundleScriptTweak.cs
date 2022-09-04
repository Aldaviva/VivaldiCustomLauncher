#nullable enable

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks; 

public class BackgroundCommonBundleScriptTweak: AbstractScriptTweak {

    private const string TWEAK_TYPE = nameof(BackgroundCommonBundleScriptTweak);

    protected internal override Task<string?> editFile(string bundleContents) => Task.Run((Func<string?>) (() => {
        string newBundleContents = bundleContents;
        newBundleContents = exposeFolderSubscriptionStatus(newBundleContents);
        newBundleContents = classifyJunkEmailAsNormalFolder(newBundleContents);
        return newBundleContents;
    }));

    /// <summary>
    /// Possible fix for https://github.com/Aldaviva/VivaldiCustomLauncher/issues/4
    /// </summary>
    public override async Task<string?> readFileAndEditIfNecessary(BaseTweakParams tweakParams) {
        string? newFileContentsToWrite = await base.readFileAndEditIfNecessary(tweakParams);
        if (newFileContentsToWrite is not null) {
            DirectoryInfo serviceWorkerScriptCacheDirectory = new(Environment.ExpandEnvironmentVariables(
                @"%LOCALAPPDATA%\Vivaldi\User Data\Default\Storage\ext\mpognobbkildjkofajifpdfhcoklimli\def\Service Worker\ScriptCache\"));
            string backupServiceWorkerScriptCacheDirectory = Path.Combine(serviceWorkerScriptCacheDirectory.Parent!.FullName, serviceWorkerScriptCacheDirectory.Name + "-old");

            try {
                Directory.Delete(backupServiceWorkerScriptCacheDirectory, true);
            } catch (DirectoryNotFoundException) { }

            Directory.Move(serviceWorkerScriptCacheDirectory.FullName, backupServiceWorkerScriptCacheDirectory);
        }

        return newFileContentsToWrite;
    }

    /// <summary>
    /// This tweak is only needed to make <see cref="BundleScriptTweak.allowMovingMailBetweenAnyFolders"/> work, since I would like to only show subscribed folders in the Move To menu.
    /// </summary>
    internal virtual string exposeFolderSubscriptionStatus(string bundleContents) => replaceOrThrow(bundleContents,
        // Object.getOwnPropertyNames(t).forEach((a=>{t[a].forEach((t=>{const{path:a,type:n}=t;s[e][a]={type:n}}))}))
        new Regex(@"(?<prefix>const{.{1,32}?}=(?<folderVar>[\w$]{1,2});[\w$]{1,2}[[\w$]{1,2}\]\[[\w$]{1,2}\]={.{1,32}?)(?<suffix>})"),
        match => $"{match.Groups["prefix"].Value},subscribed:{match.Groups["folderVar"].Value}.subscribed{CUSTOMIZED_COMMENT}{match.Groups["suffix"].Value}",
        new TweakException("Failed to find initFolders loop to add folder subscription status", TWEAK_TYPE));

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