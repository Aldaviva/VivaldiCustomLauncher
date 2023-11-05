#nullable enable

using System.IO;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks;

public abstract class BaseStreamTweak<T>: BaseTweak, Tweak<Stream, T> where T: BaseTweakParams {

    public abstract Task<Stream> readAndEditFile(T tweakParams);

    public virtual async Task saveFile(Stream stream, T tweakParams) {
        try {
            Directory.CreateDirectory(Path.GetDirectoryName(tweakParams.filename)!);

            using FileStream fileStream = File.OpenWrite(tweakParams.filename);
            await stream.CopyToAsync(fileStream);
            await fileStream.FlushAsync();
        } finally {
            stream.Close();
        }
    }

}