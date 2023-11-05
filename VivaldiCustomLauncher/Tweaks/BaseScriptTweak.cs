#nullable enable

using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace VivaldiCustomLauncher.Tweaks;

public abstract class BaseScriptTweak: BaseTweak, Tweak<string, BaseTweakParams> {

    protected const string CUSTOMIZED_COMMENT = "/* Customized by Ben */";

    /// <exception cref="TweakException" />
    public virtual async Task<string> readAndEditFile(BaseTweakParams tweakParams) {
        using FileStream file = File.Open(tweakParams.filename, FileMode.Open, FileAccess.Read);

        /*
         * Handle optional Unicode BOM, which older versions of VivaldiCustomLauncher emitted.
         * Confusingly, and contrary to the documentation, the UTF8Encoding constructor parameter "encoderShouldEmitUTF8Identifier" also controls whether the decoder consumes or ignores a BOM during parsing.
         * This means we must use a BOM-enabled decoder and a BOM-disabled encoder to fulfill our goals of reading but not writing BOMs.
         *
         * Buffer size: use same size as FileStream for ideal performance, per https://learn.microsoft.com/en-us/dotnet/api/system.io.streamreader.-ctor?view=netframework-4.8#system-io-streamreader-ctor(system-io-stream-system-text-encoding-system-boolean-system-int32-system-boolean)
         */
        using StreamReader reader = new(file, UTF8_READING, false, 4096, false);

        string bundleContents = await reader.ReadToEndAsync();

        return await editFile(bundleContents);
    }

    /// <exception cref="TweakException" />
    protected internal abstract Task<string> editFile(string bundleContents);

    public async Task saveFile(string fileContents, BaseTweakParams tweakParams) {
        using FileStream   file   = File.Open(tweakParams.filename, FileMode.Truncate, FileAccess.ReadWrite);
        using StreamWriter writer = new(file, UTF8_WRITING);
        await writer.WriteAsync(fileContents);
        await writer.FlushAsync();
    }

    /// <exception cref="TweakException"></exception>
    protected static string replaceOrThrow(string input, Regex pattern, Func<Match, string> evaluator, TweakException toThrowIfNoReplacement) =>
        replaceOrThrow(input, pattern, evaluator, 1, (pattern.Options & RegexOptions.RightToLeft) != 0 ? input.Length : 0, toThrowIfNoReplacement);

    /// <param name="count">maximum number of matches to replace, or <c>-1</c> to replace all matches</param>
    /// <exception cref="TweakException"></exception>
    protected static string replaceOrThrow(string input, Regex pattern, Func<Match, string> evaluator, int count, int startat, TweakException toThrowIfNoReplacement) {
        bool wasReplaced = false;

        string result = pattern.Replace(input, match => {
            wasReplaced = true;
            return evaluator(match);
        }, count, startat);

        if (!wasReplaced) {
            throw toThrowIfNoReplacement;
        } else {
            return result;
        }
    }

}