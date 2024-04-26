#nullable enable

using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace VivaldiCustomLauncher;

[ExcludeFromCodeCoverage]
internal static class CommandLine {

    /// <summary>
    ///     From https://stackoverflow.com/a/2611075/979493
    /// </summary>
    public static class Serializer {

        public static string argvToCommandLine(IEnumerable<string> args) {
            StringBuilder sb = new();
            foreach (string s in args) {
                sb.Append('"');
                // Escape double quotes (") and backslashes (\).
                int searchIndex = 0;
                while (true) {
                    // Put this test first to support zero length strings.
                    if (searchIndex >= s.Length) {
                        break;
                    }

                    int quoteIndex = s.IndexOf('"', searchIndex);
                    if (quoteIndex < 0) {
                        break;
                    }

                    sb.Append(s, searchIndex, quoteIndex - searchIndex);
                    escapeBackslashes(sb, s, quoteIndex - 1);
                    sb.Append('\\');
                    sb.Append('"');
                    searchIndex = quoteIndex + 1;
                }

                sb.Append(s, searchIndex, s.Length - searchIndex);
                escapeBackslashes(sb, s, s.Length - 1);
                sb.Append(@""" ");
            }

            return sb.ToString(0, Math.Max(0, sb.Length - 1));
        }

        private static void escapeBackslashes(StringBuilder sb, string s, int lastSearchIndex) {
            // Backslashes must be escaped if and only if they precede a double quote.
            for (int i = lastSearchIndex; i >= 0; i--) {
                if (s[i] != '\\') {
                    break;
                }

                sb.Append('\\');
            }
        }

    }

    public static class Parser {

        public static Arguments parse() {
            return parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
        }

        private static Arguments parse(string[] args) {
            var parser = new CommandLineApplication<Arguments> {
                UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue
            };
            parser.Conventions.UseDefaultConventions();

            parser.Parse(args);
            Arguments result = parser.Model;
            result.extras = parser.RemainingArguments;
            return result;
        }

        public class Arguments {

            [Option(LongName = "do-not-launch-vivaldi")]
            public bool noVivaldiLaunch { get; set; }

            [Option(LongName = "vivaldi-application-directory")]
            public string? vivaldiApplicationDirectory { get; set; }

            [Option("-h|--help")]
            public bool help { get; set; }

            [Option(ShortName = "?")]
            [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Set by CommandLineUtils when user passes -? as a command line argument")]
            private bool help2 {
                get => help;
                set => help = value;
            }

            public IEnumerable<string> extras { get; set; } = Array.Empty<string>();

        }

    }

}