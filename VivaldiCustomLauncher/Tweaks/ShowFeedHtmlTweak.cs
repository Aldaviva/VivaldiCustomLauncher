#nullable enable

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks;

public class ShowFeedHtmlTweak: BaseStringTweak<ShowFeedHtmlTweakParams> {

    public override Task<string> readAndEditFile(ShowFeedHtmlTweakParams tweakParams) {
        string fileContents         = File.ReadAllText(tweakParams.filename, Encoding.UTF8);
        string scriptRelativeUri    = new UriBuilder { Path = tweakParams.customScriptRelativePath }.Path;
        string modifiedFileContents = fileContents.Replace("\t</head>", $"\t\t<script src=\"{scriptRelativeUri}\"></script>\n\t</head>");

        return Task.FromResult(modifiedFileContents);
    }

}

public class ShowFeedHtmlTweakParams(string filename, string customScriptRelativePath): BaseTweakParams(filename) {

    public string customScriptRelativePath { get; } = customScriptRelativePath;

}