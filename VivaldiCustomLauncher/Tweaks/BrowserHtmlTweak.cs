#nullable enable

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks;

public class BrowserHtmlTweak: BaseStringTweak<BrowserHtmlTweakParams> {

    public override Task<string?> readFileAndEditIfNecessary(BrowserHtmlTweakParams tweakParams) {
        string fileContents          = File.ReadAllText(tweakParams.filename, Encoding.UTF8);
        string styleSheetRelativeUri = new UriBuilder { Path = tweakParams.customStyleSheetRelativeFilePath }.Path;

        bool   fileModified         = false;
        string modifiedFileContents = fileContents;

        if (!fileContents.Contains(styleSheetRelativeUri)) {
            modifiedFileContents = Regex.Replace(modifiedFileContents, @"(\s*)</head>",
                $"$1$1<link rel=\"stylesheet\" href=\"{styleSheetRelativeUri}\" />\n$0");
            fileModified = true;
        }

        if (tweakParams.customScriptRelativePath != null) {
            string scriptRelativeUri = new UriBuilder { Path = tweakParams.customScriptRelativePath }.Path;
            if (!fileContents.Contains(scriptRelativeUri)) {
                modifiedFileContents = Regex.Replace(modifiedFileContents, @"(\s*)</body>",
                    $"$1$1<script src=\"{scriptRelativeUri}\"></script>\n$0");
                fileModified = true;
            }
        }

        return Task.FromResult(fileModified ? modifiedFileContents : null);
    }

}

public class BrowserHtmlTweakParams: BaseTweakParams {

    public string customStyleSheetRelativeFilePath { get; }
    public string? customScriptRelativePath { get; }

    public BrowserHtmlTweakParams(string filename, string customStyleSheetRelativeFilePath, string? customScriptRelativePath): base(filename) {
        this.customStyleSheetRelativeFilePath = customStyleSheetRelativeFilePath;
        this.customScriptRelativePath         = customScriptRelativePath;
    }

}