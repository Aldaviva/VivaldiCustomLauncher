#nullable enable

using System.Threading.Tasks;
using VisualElementsManifest;
using VisualElementsManifest.Data;

namespace VivaldiCustomLauncher.Tweaks;

public class VisualElementsManifestTweak: Tweak<Application, VisualElementsManifestTweakParams> {

    private readonly VisualElementsManifestEditor visualElementsManifestEditor = new VisualElementsManifestEditorImpl();

    public Task<Application> readAndEditFile(VisualElementsManifestTweakParams tweakParams) => Task.Run(() => {
        Application sourceManifest = visualElementsManifestEditor.LoadFile(tweakParams.sourceFilename);
        visualElementsManifestEditor.RelativizeUris(sourceManifest, "Application");
        return sourceManifest;
    });

    public Task saveFile(Application fileContents, VisualElementsManifestTweakParams tweakParams) {
        visualElementsManifestEditor.Save(fileContents, tweakParams.filename);
        return Task.CompletedTask;
    }

}

public class VisualElementsManifestTweakParams: BaseTweakParams {

    public string sourceFilename { get; }

    public VisualElementsManifestTweakParams(string sourceFilename, string destinationFilename): base(destinationFilename) {
        this.sourceFilename = sourceFilename;
    }

}