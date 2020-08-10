using System.IO;
using System.Threading.Tasks;
using VisualElementsManifest;
using VisualElementsManifest.Data;

#nullable enable

namespace VivaldiCustomLauncher.Tweaks {

    public class VisualElementsManifestTweak: Tweak<Application, VisualElementsManifestTweakParams> {

        private readonly VisualElementsManifestEditor visualElementsManifestEditor = new VisualElementsManifestEditorImpl();

        public async Task<Application?> readFileAndEditIfNecessary(VisualElementsManifestTweakParams tweakParams) {

            Task<Application> loadSourceTask = Task.Run(() => {
                Application sourceManifest = visualElementsManifestEditor.LoadFile(tweakParams.sourceFilename);
                visualElementsManifestEditor.RelativizeUris(sourceManifest, "Application");
                return sourceManifest;
            });

            Task<Application?> loadDestinationTask = Task.Run(() => {
                try {
                    return visualElementsManifestEditor.LoadFile(tweakParams.filename);
                } catch (FileNotFoundException) {
                    return null;
                }
            });

            Application source = await loadSourceTask;
            Application? destination = await loadDestinationTask;

            return destination?.Equals(source) ?? false ? null : source;
        }

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

}