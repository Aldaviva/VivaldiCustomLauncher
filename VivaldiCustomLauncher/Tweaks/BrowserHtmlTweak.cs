#nullable enable

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks;

public class BrowserHtmlTweak: BaseStringTweak<BrowserHtmlTweakParams> {

    public override Task<string> readAndEditFile(BrowserHtmlTweakParams tweakParams) {
        string fileContents = File.ReadAllText(tweakParams.filename, Encoding.UTF8);

        string styleSheetRelativeUri = new UriBuilder { Path = tweakParams.customStyleSheetRelativeFilePath }.Path;
        string modifiedFileContents  = Regex.Replace(fileContents, @"(\s*)</head>", $"$1  <link rel=\"stylesheet\" href=\"{styleSheetRelativeUri}\" />$0");

        string scriptRelativeUri = new UriBuilder { Path = tweakParams.customScriptRelativePath }.Path;
        modifiedFileContents = Regex.Replace(modifiedFileContents, @"(\s*)</body>", $"$1  <script src=\"{scriptRelativeUri}\"></script>$0");

        return Task.FromResult(modifiedFileContents);
    }

}

public class BrowserHtmlTweakParams(string filename, string customStyleSheetRelativeFilePath, string customScriptRelativePath): BaseTweakParams(filename) {

    public string customStyleSheetRelativeFilePath { get; } = customStyleSheetRelativeFilePath;
    public string customScriptRelativePath { get; } = customScriptRelativePath;

}