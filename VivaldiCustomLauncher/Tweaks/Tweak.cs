using System.Threading.Tasks;

#nullable enable

namespace VivaldiCustomLauncher.Tweaks {

    public interface Tweak<OutputType, in Params> where Params: TweakParams where OutputType: class {

        Task<OutputType?> readFileAndEditIfNecessary(Params tweakParams);

        Task saveFile(OutputType fileContents, Params tweakParams);

    }

    public interface TweakParams {

        string filename { get; }

    }

    public class BaseTweakParams: TweakParams {

        public string filename { get; }

        public BaseTweakParams(string filename) {
            this.filename = filename;
        }

    }

}