#nullable enable

using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks;

public abstract class BaseStringTweak<T>: Tweak<string, T> where T: BaseTweakParams {

    public abstract Task<string> readAndEditFile(T tweakParams);

    public virtual async Task saveFile(string fileContents, T tweakParams) {
        Directory.CreateDirectory(Path.GetDirectoryName(tweakParams.filename)!);

        using FileStream   writeStream = File.OpenWrite(tweakParams.filename);
        using StreamWriter writer      = new(writeStream, Encoding.UTF8);
        await writer.WriteAsync(fileContents);
        await writer.FlushAsync();
    }

}