#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace VivaldiCustomLauncher;

internal class TweakedFiles {

    public string resourceDirectory { get; }
    public RelativePaths relative { get; } = new();

    private readonly IList<string> _overwrittenFiles = [];
    public IEnumerable<string> overwrittenFiles => _overwrittenFiles;

    public string customStyleSheet { get; }
    public string modStyleSheet { get; }
    public string customScript { get; }
    public string customFeedScript { get; }
    public string showFeedPage { get; }
    public string bundleScript { get; }
    public string backgroundBundleScript { get; }
    public string browserPage { get; }
    public string visualElementsSource { get; }
    public string visualElementsDestination { get; }

    public TweakedFiles(string resourceDirectory) {
        this.resourceDirectory = resourceDirectory;

        customStyleSheet          = Path.GetFullPath(Path.Combine(resourceDirectory, relative.customStyleSheet));
        modStyleSheet             = Path.GetFullPath(Path.Combine(resourceDirectory, "../../../..", "css-mods", "mods.css"));
        customScript              = Path.GetFullPath(Path.Combine(resourceDirectory, relative.customScript));
        customFeedScript          = Path.GetFullPath(Path.Combine(resourceDirectory, "rss", relative.customFeedScript));
        showFeedPage              = overwritten(Path.GetFullPath(Path.Combine(resourceDirectory, "rss", "showfeed.html")));
        bundleScript              = overwritten(Path.GetFullPath(Path.Combine(resourceDirectory, "bundle.js")));
        backgroundBundleScript    = overwritten(Path.GetFullPath(Path.Combine(resourceDirectory, "background-bundle.js")));
        browserPage               = overwritten(Path.GetFullPath(Path.Combine(resourceDirectory, "window.html")));
        visualElementsSource      = Path.GetFullPath(Path.Combine(resourceDirectory, "../../..", "vivaldi.VisualElementsManifest.xml"));
        visualElementsDestination = Path.GetFullPath(Path.Combine(resourceDirectory, "../../../..", Assembly.GetExecutingAssembly().GetName().Name + ".VisualElementsManifest.xml"));
    }

    private string overwritten(string file) {
        _overwrittenFiles.Add(file);
        return file;
    }

    public class RelativePaths {

        internal RelativePaths() { }

        public string customStyleSheet { get; } = Path.Combine("style", "custom.css");
        public string customScript { get; } = Path.Combine("scripts", "custom.js");
        public string customFeedScript { get; } = Path.Combine("..", "scripts", "custom-feed.js");

    }

}