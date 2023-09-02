#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using VivaldiCustomLauncher.Tweaks;

namespace VivaldiCustomLauncher;

public static class VivaldiLauncher {

    private static readonly VersionNumberComparer VERSION_NUMBER_COMPARER = new();
    private static          HttpClient?           cachedHttpClient;
    public static readonly  AssemblyName          ASSEMBLY_NAME = Assembly.GetExecutingAssembly().GetName();

    private static HttpClient httpClient => cachedHttpClient ??= new HttpClient(new HttpClientHandler {
        MaxConnectionsPerServer = 24,
        AllowAutoRedirect       = true
    }) {
        Timeout               = TimeSpan.FromSeconds(10),
        DefaultRequestHeaders = { UserAgent = { new ProductInfoHeaderValue(ASSEMBLY_NAME.Name, ASSEMBLY_NAME.Version.ToString()) } }
    };

    [STAThread]
    public static async Task<int> Main() {
        Application.EnableVisualStyles();

        Application.ThreadException                += (_, args) => onUncaughtException(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) => onUncaughtException((Exception) args.ExceptionObject);

        try {
            return await tweakAndLaunch() ? 0 : 1;
        } finally {
            cachedHttpClient?.Dispose();
        }
    }

    private static async Task<bool> tweakAndLaunch() {
        Stopwatch stopwatch = Stopwatch.StartNew();
        bool      success   = true;

        try {
            CommandLine.Parser.Arguments arguments = CommandLine.Parser.parse();
            if (arguments.help) {
                using Process currentProcess      = Process.GetCurrentProcess();
                string        selfProcessFilename = currentProcess.ProcessName;
                if (!Path.HasExtension(selfProcessFilename)) {
                    selfProcessFilename = Path.ChangeExtension(selfProcessFilename, "exe");
                }

                string usage = $"""
                                Example:

                                {selfProcessFilename} [--vivaldi-application-directory="C:\Program Files\Vivaldi\Application"] [--do-not-launch-vivaldi] ["https://vivaldi.com"] [<extra>..]
                                            
                                Parameters:

                                --vivaldi-application-directory="dir"
                                   The absolute path of the Application directory inside
                                   Vivaldi's installation directory. If dir contains a space, make
                                   sure to surround it with double quotation marks. If omitted,
                                   it will be detected automatically from the registry.

                                --do-not-launch-vivaldi
                                   Install tweaks as needed, but do not launch Vivaldi. If
                                   omitted, Vivaldi will be launched after installing tweaks.

                                url
                                   The web page that Vivaldi should load. If omitted, Vivaldi
                                   will use its configured startup behavior, or open a new tab
                                   if it was already running.

                                <extra>
                                   Any unrecognized parameters will be passed on to Vivaldi,
                                   such as --debug-packed-apps --enable-logging --v=1.

                                -?, -h, --help
                                   Show this usage information dialog box.
                                """;

                MessageBox.Show(usage, $"{ASSEMBLY_NAME.Name} usage", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }

            string processToRun = Path.Combine(getVivaldiApplicationDirectory(arguments.vivaldiApplicationDirectory), "vivaldi.exe");

            try {
                using Process? existingVivaldiProcess = Process.GetProcessesByName("vivaldi").FirstOrDefault();
                if (existingVivaldiProcess == null) {
                    bool isInstallationPending = await new ProgramUpgrader(httpClient, VERSION_NUMBER_COMPARER).upgrade();
                    if (isInstallationPending) {
                        return true;
                    }

                    string resourceDirectory = getResourceDirectory(Path.GetDirectoryName(processToRun)!);
                    await applyTweaks(resourceDirectory);
                }

            } catch (TweakException e) {
                MessageBox.Show($"Failed to apply tweak {e.tweakTypeName}.{e.tweakMethodName}: {e.bareMessage}", "Failed to tweak Vivaldi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                success = false;
            }

            IEnumerable<string> originalArguments     = arguments.extras;
            string              processArgumentsToRun = CommandLine.Serializer.argvToCommandLine(customizeArguments(originalArguments));

            if (!arguments.noVivaldiLaunch) {
                createProcess(processToRun, processArgumentsToRun);
            }

            stopwatch.Stop();
            // MessageBox.Show($"Started {processToRun} {processArgumentsToRun} in {stopwatch.ElapsedMilliseconds:N0} ms", "VivaldiCustomLauncher", MessageBoxButtons.OK, MessageBoxIcon.Information);

        } catch (InvalidOperationException e) {
            MessageBox.Show(e.Message, "Failed to launch Vivaldi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            success = false;
        } catch (Exception e) when (e is not OutOfMemoryException) {
            onUncaughtException(e);
            success = false;
        }

        return success;
    }

    private static void onUncaughtException(Exception e) {
        string message = (e as AggregateException)?.InnerException?.Message ?? e.Message;
        MessageBox.Show($"{e.GetType().Name}: {message}\n\n{e.StackTrace}", "Failed to tweak and launch Vivaldi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <exception cref="TweakException"></exception>
    /// <exception cref="Exception">Ignore.</exception>
    private static Task applyTweaks(string resourceDirectory) {
        string customStyleSheetRelativePath = Path.Combine("style", "custom.css");
        string customStyleSheetAbsolutePath = Path.Combine(resourceDirectory, customStyleSheetRelativePath);

        string customScriptRelativePath = Path.Combine("scripts", "custom.js");
        string customScriptAbsolutePath = Path.Combine(resourceDirectory, customScriptRelativePath);

        string bundleScriptAbsolutePath                 = Path.Combine(resourceDirectory, "bundle.js");
        string backgroundBundleCommonScriptAbsolutePath = Path.Combine(resourceDirectory, "background-common-bundle.js");
        string browserPageAbsolutePath                  = Path.Combine(resourceDirectory, "browser.html");
        string windowPageAbsolutePath                   = Path.Combine(resourceDirectory, "window.html");

        string showFeedPageAbsolutePath     = Path.Combine(resourceDirectory, "rss", "showfeed.html");
        string customFeedScriptRelativePath = Path.Combine("..", "scripts", "custom-feed.js");
        string customFeedScriptAbsolutePath = Path.Combine(resourceDirectory, "rss", customFeedScriptRelativePath);

        string visualElementsSourcePath      = Path.Combine(resourceDirectory, "../../..", "vivaldi.VisualElementsManifest.xml");
        string visualElementsDestinationPath = Path.Combine(resourceDirectory, "../../../..", ASSEMBLY_NAME.Name + ".VisualElementsManifest.xml");

        try {
            return Task.WhenAll(
                applyTweakIfNecessary(new BrowserHtmlTweak(), new BrowserHtmlTweakParams(browserPageAbsolutePath, customStyleSheetRelativePath, customScriptRelativePath)),
                applyTweakIfNecessary(new BrowserHtmlTweak(), new BrowserHtmlTweakParams(windowPageAbsolutePath, customStyleSheetRelativePath, customScriptRelativePath)),
                applyTweakIfNecessary(new CustomStyleSheetTweak(httpClient), new BaseTweakParams(customStyleSheetAbsolutePath)),
                applyTweakIfNecessary(new BundleScriptTweak(), new BaseTweakParams(bundleScriptAbsolutePath)),
                applyTweakIfNecessary(new BackgroundCommonBundleScriptTweak(), new BaseTweakParams(backgroundBundleCommonScriptAbsolutePath)),
                applyTweakIfNecessary(new CustomScriptTweak(httpClient), new BaseTweakParams(customScriptAbsolutePath)),
                applyTweakIfNecessary(new VisualElementsManifestTweak(), new VisualElementsManifestTweakParams(visualElementsSourcePath, visualElementsDestinationPath)),
                applyTweakIfNecessary(new ShowFeedHtmlTweak(), new ShowFeedHtmlTweakParams(showFeedPageAbsolutePath, customFeedScriptRelativePath)),
                applyTweakIfNecessary(new CustomFeedScriptTweak(httpClient), new BaseTweakParams(customFeedScriptAbsolutePath))
            );
        } catch (AggregateException e) {
            if (e.InnerExceptions.Where(exception => exception is TweakException).Cast<TweakException>().FirstOrDefault() is { } tweakException) {
                throw tweakException;
            } else {
                throw e.InnerException!;
            }
        }
    }

    /// <exception cref="TweakException"></exception>
    private static async Task applyTweakIfNecessary<OutputType, Params>(Tweak<OutputType, Params> tweak, Params tweakParams) where Params: TweakParams where OutputType: class {
        OutputType? editedFile = await tweak.readFileAndEditIfNecessary(tweakParams);
        if (editedFile != null) {
            await tweak.saveFile(editedFile, tweakParams);
        }
    }

    /// <exception cref="InvalidOperationException"></exception>
    private static string getVivaldiApplicationDirectory(string? vivaldiApplicationDirectoryCommandLineArgument = null) {
        if (vivaldiApplicationDirectoryCommandLineArgument != null) {
            return vivaldiApplicationDirectoryCommandLineArgument;
        }

        if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\vivaldi.exe", "Path", null) is string appPath) {
            return appPath;
        }

        const string UNINSTALL_REGISTRY_PATH       = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Vivaldi";
        const string UNINSTALL_REGISTRY_PATH_WOW64 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Vivaldi";
        IEnumerable<(RegistryKey hive, string path)> uninstallKeys = new[] {
            (Registry.CurrentUser, UNINSTALL_REGISTRY_PATH),
            (Registry.CurrentUser, UNINSTALL_REGISTRY_PATH_WOW64),
            (Registry.LocalMachine, UNINSTALL_REGISTRY_PATH),
            (Registry.LocalMachine, UNINSTALL_REGISTRY_PATH_WOW64)
        };

        foreach ((RegistryKey hive, string path) in uninstallKeys) {
            using RegistryKey? key = hive.OpenSubKey(path, false);
            if (key != null) {
                return (string) key.GetValue("InstallLocation");
            }
        }

        throw new InvalidOperationException("Could not find Vivaldi uninstallation key in registry");
    }

    private static string getResourceDirectory(string applicationDirectory) {
        string versionDirectory = Directory.EnumerateDirectories(applicationDirectory)
            .Where(absoluteSubdirectory => {
                string relativeSubdirectory = Path.GetFileName(absoluteSubdirectory)!;
                return Regex.IsMatch(relativeSubdirectory, @"\A(?:\d+\.){3}\d+\z");
            })
            .OrderByDescending(Path.GetFileName, VERSION_NUMBER_COMPARER)
            .First();

        return Path.Combine(versionDirectory, "resources", "vivaldi");
    }

    private static IEnumerable<string> customizeArguments(IEnumerable<string> originalArguments) {
        IList<string> customizedArguments = originalArguments.ToList();
        // Turning on web accessibility makes Vivaldi 3 very slow, so use my WebAutoType fork that gets current URL posted from custom.js using Ajax.
        // customizedArguments.Insert(0, "--force-renderer-accessibility");
        return customizedArguments;
    }

    private static int createProcess(string process, string arguments) {
        using Process? createdProcess = Process.Start(process, arguments);
        return createdProcess?.Id ?? -1;
    }

}