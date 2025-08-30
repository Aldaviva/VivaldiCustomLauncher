#nullable enable

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks;

public abstract class BaseScriptTweak: Tweak<string, BaseTweakParams> {

    protected const string CUSTOMIZED_COMMENT = "/* Customized by Ben */";

    /// <exception cref="TweakException" />
    public virtual async Task<string> readAndEditFile(BaseTweakParams tweakParams) {
        using FileStream file = File.Open(tweakParams.filename, FileMode.Open, FileAccess.Read);

        /*
         * Buffer size: use same size as FileStream for ideal performance, per https://learn.microsoft.com/en-us/dotnet/api/system.io.streamreader.-ctor?view=netframework-4.8#system-io-streamreader-ctor(system-io-stream-system-text-encoding-system-boolean-system-int32-system-boolean)
         */
        using StreamReader reader = new(file, Encoding.UTF8, false, 4096, false);

        string bundleContents = await reader.ReadToEndAsync();

        return await editFile(bundleContents);
    }

    /// <exception cref="TweakException" />
    protected internal abstract Task<string> editFile(string bundleContents);

    public async Task saveFile(string fileContents, BaseTweakParams tweakParams) {
        using FileStream   file   = File.Open(tweakParams.filename, FileMode.Truncate, FileAccess.ReadWrite);
        using StreamWriter writer = new(file, Encoding.UTF8);
        await writer.WriteAsync(fileContents);
        await writer.FlushAsync();
    }

    /// <exception cref="TweakException"></exception>
    protected static string replaceOrThrow(string input, Regex pattern, Func<Match, string> evaluator, TweakException toThrowIfNoReplacement) =>
        replaceOrThrow(input, pattern, evaluator, 1, (pattern.Options & RegexOptions.RightToLeft) != 0 ? input.Length : 0, toThrowIfNoReplacement);

    /// <param name="count">maximum number of matches to replace, or <c>-1</c> to replace all matches</param>
    /// <exception cref="TweakException"></exception>
    protected static string replaceOrThrow(string input, Regex pattern, Func<Match, string> evaluator, int count, int startat, TweakException toThrowIfNoReplacement) {
        int actualCount = 0;

        string result = pattern.Replace(input, match => {
            actualCount++;
            return evaluator(match);
        }, count, startat);

        if (actualCount != count) {
            throw toThrowIfNoReplacement;
        } else {
            return result;
        }
    }

}