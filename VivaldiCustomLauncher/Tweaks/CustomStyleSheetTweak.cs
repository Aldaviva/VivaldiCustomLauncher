using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

#nullable enable

namespace VivaldiCustomLauncher.Tweaks {

    public class CustomStyleSheetTweak: Tweak<Stream, BaseTweakParams> {

        private readonly HttpClient httpClient;

        public CustomStyleSheetTweak(HttpClient httpClient) {
            this.httpClient = httpClient;
        }

        public async Task<Stream?> readFileAndEditIfNecessary(BaseTweakParams tweakParams) {
            return !File.Exists(tweakParams.filename)
                ? await httpClient.GetStreamAsync(@"https://gist.githubusercontent.com/Aldaviva/9fbe321331b7f80786a371e0fd4bcfaf/raw/%257C%2520style%255Ccustom.css") : null;
        }

        public async Task saveFile(Stream downloadStream, BaseTweakParams tweakParams) {
            try {
                using FileStream fileStream = File.OpenWrite(tweakParams.filename);
                await downloadStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            } finally {
                downloadStream.Close();
            }
        }

    }

}