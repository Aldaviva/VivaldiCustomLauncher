using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

#nullable enable

namespace VivaldiCustomLauncher.Tweaks {

    public class CustomScriptTweak: Tweak<Stream, BaseTweakParams> {

        private readonly HttpClient httpClient;

        public CustomScriptTweak(HttpClient httpClient) {
            this.httpClient = httpClient;
        }

        public async Task<Stream?> readFileAndEditIfNecessary(BaseTweakParams tweakParams) {
            return !File.Exists(tweakParams.filename)
                ? await httpClient.GetStreamAsync(@"https://github.com/Aldaviva/VivaldiCustomResources/raw/master/scripts/custom.js") : null;
        }

        public async Task saveFile(Stream downloadStream, BaseTweakParams tweakParams) {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(tweakParams.filename) ?? throw new InvalidOperationException("Could not get path of custom script file " + tweakParams.filename));

                using FileStream fileStream = File.OpenWrite(tweakParams.filename);
                await downloadStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            } finally {
                downloadStream.Close();
            }
        }

    }

}