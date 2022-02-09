using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#nullable enable

namespace VivaldiCustomLauncher.Tweaks {

    public abstract class AbstractScriptTweak: Tweak<string, BaseTweakParams> {

        private static readonly char[] EXPECTED_HEADER = CUSTOMIZED_COMMENT.ToCharArray();

        protected const string CUSTOMIZED_COMMENT = @"/* Customized by Ben */";

        /// <exception cref="TweakException"></exception>
        public async Task<string?> readFileAndEditIfNecessary(BaseTweakParams tweakParams) {
            string           bundleContents;
            using FileStream file = File.Open(tweakParams.filename, FileMode.Open, FileAccess.Read);

            using (StreamReader reader = new(file, Encoding.UTF8, false, 4 * 1024, true)) {
                char[] buffer = new char[EXPECTED_HEADER.Length];
                await reader.ReadAsync(buffer, 0, buffer.Length);

                if (EXPECTED_HEADER.SequenceEqual(buffer)) {
                    return null;
                }

                file.Seek(0, SeekOrigin.Begin);
                reader.DiscardBufferedData();
                bundleContents = await reader.ReadToEndAsync();
            }

            return await editFile(bundleContents);
        }

        /// <exception cref="TweakException"></exception>
        protected abstract Task<string?> editFile(string bundleContents);

        public async Task saveFile(string fileContents, BaseTweakParams tweakParams) {
            using FileStream   file   = File.Open(tweakParams.filename, FileMode.Truncate, FileAccess.ReadWrite);
            using StreamWriter writer = new(file, Encoding.UTF8);
            await writer.WriteAsync(EXPECTED_HEADER);
            await writer.WriteAsync(fileContents);
            await writer.FlushAsync();
        }

        protected static string? emptyToNull(string input) => string.IsNullOrEmpty(input) ? null : input;

        /// <exception cref="TweakException"></exception>
        protected static string replaceOrThrow(string input, Regex pattern, Func<Match, string> evaluator, TweakException toThrowIfNoReplacement) => replaceOrThrow(input, pattern, evaluator, -1,
            (pattern.Options & RegexOptions.RightToLeft) != 0 ? input.Length : 0, toThrowIfNoReplacement);

        /// <exception cref="TweakException"></exception>
        protected static string replaceOrThrow(string input, Regex pattern, Func<Match, string> evaluator, int count, int startat, TweakException toThrowIfNoReplacement) {
            bool wasReplaced = false;

            string result = pattern.Replace(input, match => {
                wasReplaced = true;
                return evaluator(match);
            });

            if (!wasReplaced) {
                throw toThrowIfNoReplacement;
            } else {
                return result;
            }
        }

    }

}