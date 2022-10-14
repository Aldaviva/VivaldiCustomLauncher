#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EntryPoint;

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

        public static Arguments parse(string[] args) {
            return Cli.Parse<Arguments>(args);
        }

        public class Arguments: BaseCliArguments {

            public Arguments(): base(VivaldiLauncher.ASSEMBLY_NAME.Name) { }

            public override void OnHelpInvoked(string helpText) {
                using Process currentProcess      = Process.GetCurrentProcess();
                string        selfProcessFilename = currentProcess.ProcessName;
                if (!Path.HasExtension(selfProcessFilename)) {
                    selfProcessFilename = Path.ChangeExtension(selfProcessFilename, "exe");
                }

                string usage = $@"Example:

{selfProcessFilename} [--vivaldi-application-directory=""C:\Program Files\Vivaldi\Application""] [--do-not-launch-vivaldi] [""https://vivaldi.com""] [<extra>..]
            
Parameters:

--vivaldi-application-directory=""dir""
   The absolute path of the Application directory inside Vivaldi's installation directory.
   If dir contains a space, make sure to surround it with double quotation marks.
   If omitted, it will be detected automatically from the registry.

--do-not-launch-vivaldi
   Install tweaks as needed, but do not launch Vivaldi.
   If omitted, Vivaldi will be launched after installing tweaks.

url
   The web page that Vivaldi should load.
   If omitted, Vivaldi will use its configured startup behavior, or open a new tab if it was already running.

<extra>
   Any unrecognized parameters will be passed on to Vivaldi, such as --debug-packed-apps --enable-logging --v=1.

-?, -h, --help
   Show this usage information dialog box.";

                MessageBox.Show(usage, "VivaldiCustomLauncher usage", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            [Option("do-not-launch-vivaldi")]
            public bool doNotLaunchVivaldi { get; set; }

            [OptionParameter("vivaldi-application-directory")]
            public string? vivaldiApplicationDirectory { get; set; }

            [Option('?')]
            private bool alternateHelp {
                get => HelpInvoked;
                set => HelpInvoked = value;
            }

        }

    }

}