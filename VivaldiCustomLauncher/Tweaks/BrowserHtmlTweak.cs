#nullable enable

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks;

public class BrowserHtmlTweak: BaseStringTweak<BrowserHtmlTweakParams> {

    public override Task<string?> readFileAndEditIfNecessary(BrowserHtmlTweakParams tweakParams) {
        string fileContents          = File.ReadAllText(tweakParams.filename, Encoding.UTF8);
        string styleSheetRelativeUri = new UriBuilder { Path = tweakParams.customStyleSheetRelativeFilePath }.Path;
        string scriptRelativeUri     = new UriBuilder { Path = tweakParams.customScriptRelativePath }.Path;

        bool   fileModified         = false;
        string modifiedFileContents = fileContents;

        if (!fileContents.Contains(styleSheetRelativeUri)) {
            modifiedFileContents = modifiedFileContents.Replace(@"  </head>",
                $"    <link rel=\"stylesheet\" href=\"{styleSheetRelativeUri}\" />\n  </head>");
            fileModified = true;
        }

        if (!fileContents.Contains(scriptRelativeUri)) {
            modifiedFileContents = modifiedFileContents.Replace(@"  </body>",
                $"    <script src=\"{scriptRelativeUri}\"></script>\n  </body>");
            fileModified = true;
        }

        return Task.FromResult(fileModified ? modifiedFileContents : null);
    }

}

public class BrowserHtmlTweakParams: BaseTweakParams {

    public string customStyleSheetRelativeFilePath { get; }
    public string customScriptRelativePath { get; }

    public BrowserHtmlTweakParams(string filename, string customStyleSheetRelativeFilePath, string customScriptRelativePath): base(filename) {
        this.customStyleSheetRelativeFilePath = customStyleSheetRelativeFilePath;
        this.customScriptRelativePath         = customScriptRelativePath;
    }

}