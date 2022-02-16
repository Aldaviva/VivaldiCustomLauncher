using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#nullable enable

namespace VivaldiCustomLauncher.Tweaks {

    public class BackgroundCommonBundleScriptTweak: AbstractScriptTweak {

        protected internal override Task<string?> editFile(string bundleContents) => Task.Run((Func<string?>) (() => {
            string newBundleContents = bundleContents;
            newBundleContents = exposeFolderSubscriptionStatus(newBundleContents);
            return newBundleContents;
        }));

        /// <summary>
        /// This tweak is only needed to make <see cref="BundleScriptTweak.allowMovingMailBetweenAnyFolders"/> work, since I would like to only show subscribed folders in the Move To menu.
        /// </summary>
        internal virtual string exposeFolderSubscriptionStatus(string bundleContents) {
            return Regex.Replace(bundleContents,
                // Object.getOwnPropertyNames(t).forEach((a=>{t[a].forEach((t=>{const{path:a,type:n}=t;s[e][a]={type:n}}))}))
                @"(?<prefix>const{.{1,32}?}=(?<folderVar>[\w$]{1,2});[\w$]{1,2}[[\w$]{1,2}\]\[[\w$]{1,2}\]={.{1,32}?)(?<suffix>})",
                match => $"{match.Groups["prefix"].Value},subscribed:{match.Groups["folderVar"].Value}.subscribed{CUSTOMIZED_COMMENT}{match.Groups["suffix"].Value}");
        }

    }

}