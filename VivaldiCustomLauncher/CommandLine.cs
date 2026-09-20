#nullable enable

using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.HelpText;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;

namespace VivaldiCustomLauncher;

[ExcludeFromCodeCoverage]
internal static class CommandLine {

    public static (Arguments args, CommandLineApplication<Arguments> argsParser) parse() {
        return parse(Environment.GetCommandLineArgs().Skip(1).ToArray());
    }

    private static (Arguments args, CommandLineApplication<Arguments> argsParser) parse(string[] args) {
        using Process currentProcess      = Process.GetCurrentProcess();
        string        selfProcessFilename = currentProcess.ProcessName;
        if (!Path.HasExtension(selfProcessFilename)) {
            selfProcessFilename = Path.ChangeExtension(selfProcessFilename, "exe");
        }

        var parser = new CommandLineApplication<Arguments> {
            FullName                     = "Vivaldi Custom Launcher",
            Description                  = "Tweak and launch a Vivaldi installation.",
            UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.CollectAndContinue,
            ExtendedHelpText = $"""
                  <url>                                  The web page that 
                                                         Vivaldi should load. If 
                                                         omitted, Vivaldi will 
                                                         use its configured 
                                                         startup behavior, or 
                                                         open a new tab if it 
                                                         was already running.
                  <extra>                                Any unrecognized 
                                                         parameters will be 
                                                         passed on to Vivaldi, 
                                                         such as 
                                                         --debug-packed-apps 
                                                         --enable-logging --v=1.

                Example:
                  {selfProcessFilename} [--vivaldi-application-directory="C:\Program Files\Vivaldi\Application"] [--do-not-launch-vivaldi] [--untweak] ["https://vivaldi.com"] [<extra>..]
                """
        };
        parser.Conventions.UseDefaultConventions();
        (parser.HelpTextGenerator as DefaultHelpTextGenerator)?.MaxLineLength = 64;

        parser.Parse(args);
        Arguments result = parser.Model;
        result.extras = parser.RemainingArguments;

        if (parser.IsShowingInformation) {
            MessageBox.Show(parser.GetHelpText(), "Vivaldi Custom Launcher usage", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        return (args: result, argsParser: parser);
    }

    public class Arguments {

        [Option("--do-not-launch-vivaldi", Description = "Install tweaks as needed, but do not launch Vivaldi. If omitted, Vivaldi will be launched after installing tweaks.")]
        public bool noVivaldiLaunch { get; set; }

        [Option("--vivaldi-application-directory",
            Description =
                "The absolute path of the Application directory inside Vivaldi's installation directory. If <dir> contains a space, make sure to surround it with double quotation marks. If omitted, it will be detected automatically from the registry.",
            ValueName = "dir")]
        public string? vivaldiApplicationDirectory { get; set; }

        [Option("--untweak", Description = "Remove all installed tweaks. Easier than reinstalling Vivaldi if the tweaks are causing problems.")]
        public bool untweak { get; set; }

        [Option("--intercept-update-notifier", "Launch update_notifier.exe, or use the existing process with <pid>, to watch for Vivaldi upgrades to tweak.", CommandOptionType.SingleOrNoValue,
            ValueName = "pid")]
        public (bool active, int? pid) interceptUpdateNotifier { get; set; }

        [Option("--install-upgrade-interceptor", Description = "Intercept Vivaldi upgrades to automatically tweak the upgraded version, without having to exit and manually run this program.")]
        public bool installUpgradeInterceptor { get; set; }

        public IReadOnlyList<string> extras { get; set; } = [];

    }

}