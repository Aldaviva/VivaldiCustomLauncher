#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks;

public class ShowFeedHtmlTweak: BaseStringTweak<ShowFeedHtmlTweakParams> {

    public override Task<string> readAndEditFile(ShowFeedHtmlTweakParams tweakParams) {
        string fileContents         = File.ReadAllText(tweakParams.filename, UTF8_READING);
        string scriptRelativeUri    = new UriBuilder { Path = tweakParams.customScriptRelativePath }.Path;
        string modifiedFileContents = fileContents.Replace("\t</head>", $"\t\t<script src=\"{scriptRelativeUri}\"></script>\n\t</head>");

        return Task.FromResult(modifiedFileContents);
    }

}

public class ShowFeedHtmlTweakParams: BaseTweakParams {

    public string customScriptRelativePath { get; }

    public ShowFeedHtmlTweakParams(string filename, string customScriptRelativePath): base(filename) {
        this.customScriptRelativePath = customScriptRelativePath;
    }

}