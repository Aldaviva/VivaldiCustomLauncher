#nullable enable

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks;

public class ShowFeedHtmlTweak: BaseStringTweak<ShowFeedHtmlTweakParams> {

    public override Task<string?> readFileAndEditIfNecessary(ShowFeedHtmlTweakParams tweakParams) {
        string fileContents      = File.ReadAllText(tweakParams.filename, new UTF8Encoding(true, true));
        string scriptRelativeUri = new UriBuilder { Path = tweakParams.customScriptRelativePath }.Path;

        bool   fileModified         = false;
        string modifiedFileContents = fileContents;

        if (!fileContents.Contains(scriptRelativeUri)) {
            modifiedFileContents = modifiedFileContents.Replace("\t</head>",
                $"\t\t<script src=\"{scriptRelativeUri}\"></script>\n\t</head>");
            fileModified = true;
        }

        return Task.FromResult(fileModified ? modifiedFileContents : null);
    }

}

public class ShowFeedHtmlTweakParams: BaseTweakParams {

    public string customScriptRelativePath { get; }

    public ShowFeedHtmlTweakParams(string filename, string customScriptRelativePath): base(filename) {
        this.customScriptRelativePath = customScriptRelativePath;
    }

}