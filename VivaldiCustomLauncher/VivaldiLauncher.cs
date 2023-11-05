#nullable enable

using Microsoft.Win32;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Utf8Json;
using VivaldiCustomLauncher.Tweaks;

namespace VivaldiCustomLauncher;

public static class VivaldiLauncher {

    // private static readonly VersionNumberComparer VERSION_NUMBER_COMPARER = new();
    private static         HttpClient?  cachedHttpClient;
    public static readonly AssemblyName CURRENT_ASSEMBLY = Assembly.GetExecutingAssembly().GetName();

    private static HttpClient httpClient => cachedHttpClient ??= new HttpClient(new HttpClientHandler {
        MaxConnectionsPerServer = 24,
        AllowAutoRedirect       = true,
        AutomaticDecompression  = DecompressionMethods.GZip
    }) {
        Timeout               = TimeSpan.FromSeconds(10),
        DefaultRequestHeaders = { UserAgent = { new ProductInfoHeaderValue(CURRENT_ASSEMBLY.Name, CURRENT_ASSEMBLY.Version.ToString()) } }
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

                MessageBox.Show(usage, $"{CURRENT_ASSEMBLY.Name} usage", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }

            string vivaldiApplicationDirectory = getVivaldiApplicationDirectory(arguments.vivaldiApplicationDirectory);
            string processToRun                = Path.Combine(vivaldiApplicationDirectory, "vivaldi.exe");

            using Process? existingVivaldiProcess = Process.GetProcessesByName("vivaldi").FirstOrDefault();
            if (existingVivaldiProcess == null) {
                GitHubClient           gitHub                      = new(httpClient);
                Task<bool>             isInstallationPendingTask   = new ProgramUpgrader(gitHub).upgrade();
                Task<string?>          resourcesRepoCommitHashTask = gitHub.fetchLatestCommitHash("Aldaviva", "VivaldiCustomResources");
                string                 tweakManifestAbsolutePath   = Path.Combine(vivaldiApplicationDirectory, "VivaldiCustomLauncher.manifest.json");
                Task<VersionManifest?> versionManifestTask         = readVersionManifest(tweakManifestAbsolutePath);

                bool isInstallationPending = await isInstallationPendingTask;
                if (isInstallationPending) {
                    Console.WriteLine("Upgrading VivaldiCustomLauncher to a new version");
                    return true;
                }

                string? resourcesRepoCommitHash = await resourcesRepoCommitHashTask;
                if (resourcesRepoCommitHash != null) {
                    Console.WriteLine($"Latest resources repo commit hash is {resourcesRepoCommitHash}");
                    string       resourceDirectory = getResourceDirectory(Path.GetDirectoryName(processToRun)!);
                    TweakedFiles tweakedFiles      = new(resourceDirectory);

                    VersionManifest? versionManifest = await versionManifestTask;
                    bool shouldUnapplyTweaks = versionManifest == null
                        ? File.Exists(Path.Combine(resourceDirectory, tweakedFiles.relative.customScript)) // just upgraded to first launcher version that uses manifest files
                        : resourcesRepoCommitHash != versionManifest.resourcesCommitHash ||
                        CURRENT_ASSEMBLY.Version != versionManifest.launcherVersion; // tweaks are from different launcher or resources

                    if (shouldUnapplyTweaks) {
                        Console.WriteLine("Unapplying tweaks");
                        string installerFile = Path.GetFullPath(Path.Combine(resourceDirectory, @"..\..\Installer\vivaldi.7z"));
                        unapplyTweaks(vivaldiApplicationDirectory, installerFile, tweakedFiles);
                    }

                    bool shouldApplyTweaks = versionManifest == null || shouldUnapplyTweaks;
                    if (shouldApplyTweaks) {
                        using FileStream manifestWriteStream = new(tweakManifestAbsolutePath, FileMode.Create, FileAccess.Write, FileShare.None);
                        VersionManifest  newManifest         = new() { launcherVersion = CURRENT_ASSEMBLY.Version, resourcesCommitHash = resourcesRepoCommitHash };

                        try {
                            Console.WriteLine("Applying tweaks");
                            await applyTweaks(tweakedFiles);
                            Console.WriteLine($"Writing manifest file to {tweakManifestAbsolutePath}");
                            await JsonSerializer.SerializeAsync(manifestWriteStream, newManifest);
                            await manifestWriteStream.FlushAsync();
                        } catch (TweakException e) {
                            MessageBox.Show($"Failed to apply tweak {e.tweakTypeName}.{e.tweakMethodName}: {e.bareMessage}", "Failed to tweak Vivaldi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            success = false;
                        }
                    }
                } else {
                    success = false;
                }
            } else {
                Console.WriteLine("Vivaldi is already running, not applying tweaks.");
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
    private static Task applyTweaks(TweakedFiles files) {
        try {
            return Task.WhenAll(
                applyTweak(new BrowserHtmlTweak(), new BrowserHtmlTweakParams(files.browserPage, files.relative.customStyleSheet, files.relative.customScript)),
                applyTweak(new CustomStyleSheetTweak(httpClient), new BaseTweakParams(files.customStyleSheet)),
                applyTweak(new BundleScriptTweak(), new BaseTweakParams(files.bundleScript)),
                applyTweak(new BackgroundCommonBundleScriptTweak(), new BaseTweakParams(files.backgroundCommonBundleScript)),
                applyTweak(new CustomScriptTweak(httpClient), new BaseTweakParams(files.customScript)),
                applyTweak(new VisualElementsManifestTweak(), new VisualElementsManifestTweakParams(files.visualElementsSource, files.visualElementsDestination)),
                applyTweak(new ShowFeedHtmlTweak(), new ShowFeedHtmlTweakParams(files.showFeedPage, files.relative.customFeedScript)),
                applyTweak(new CustomFeedScriptTweak(httpClient), new BaseTweakParams(files.customFeedScript))
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
    private static async Task applyTweak<OutputType, Params>(Tweak<OutputType, Params> tweak, Params tweakParams) where Params: TweakParams where OutputType: class {
        OutputType editedFile = await tweak.readAndEditFile(tweakParams);
        await tweak.saveFile(editedFile, tweakParams);
        Console.WriteLine($"Tweaked {editedFile}");
    }

    private static void unapplyTweaks(string vivaldiApplicationDirectory, string installerArchiveAbsolutePath, TweakedFiles files) {
        using SevenZipArchive installerArchive = SevenZipArchive.Open(installerArchiveAbsolutePath, new ReaderOptions { DisableCheckIncomplete = true }); // takes about 1 second

        foreach (string fileToRestore in files.overwrittenFiles) {
            string                filenameInArchive = "Vivaldi-bin/" + fileToRestore.Remove(0, vivaldiApplicationDirectory.Length).Replace('\\', '/').TrimStart('/');
            SevenZipArchiveEntry? fileInArchive     = installerArchive.Entries.FirstOrDefault(entry => string.Equals(entry.Key, filenameInArchive, StringComparison.OrdinalIgnoreCase));
            if (fileInArchive != null) {
                Console.WriteLine($"Extracting {filenameInArchive} to {fileToRestore}");
            } else {
                Console.WriteLine($"Could not find {filenameInArchive} in {installerArchiveAbsolutePath}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fileToRestore));
            fileInArchive?.WriteToFile(fileToRestore, new ExtractionOptions { Overwrite = true, PreserveAttributes = true });
        }
    }

    /// <exception cref="InvalidOperationException"></exception>
    private static string getVivaldiApplicationDirectory(string? vivaldiApplicationDirectoryCommandLineArgument = null) {
        string? applicationDirectory = null;

        if (vivaldiApplicationDirectoryCommandLineArgument != null) {
            applicationDirectory = vivaldiApplicationDirectoryCommandLineArgument.TrimEnd('\\', '/');
        } else if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\vivaldi.exe", "Path", null) is string appPath) {
            applicationDirectory = appPath; // no trailing slash
        } else {
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
                    applicationDirectory = (string) key.GetValue("InstallLocation"); // no trailing slash
                    break;
                }
            }
        }

        if (applicationDirectory != null) {
            return Path.GetFullPath(applicationDirectory);
        } else {
            throw new InvalidOperationException("Could not find Vivaldi uninstallation key in registry");
        }
    }

    private static string getResourceDirectory(string applicationDirectory) {
        string versionDirectory = Directory.EnumerateDirectories(applicationDirectory)
            .Where(absoluteSubdirectory => {
                string relativeSubdirectory = Path.GetFileName(absoluteSubdirectory)!;
                return Regex.IsMatch(relativeSubdirectory, @"\A(?:\d+\.){3}\d+\z");
            })
            .OrderByDescending(s => Version.Parse(Path.GetFileName(s)))
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

    private static async Task<VersionManifest?> readVersionManifest(string manifestFile) {
        try {
            using FileStream file = File.Open(manifestFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await JsonSerializer.DeserializeAsync<VersionManifest>(file);
        } catch (FileNotFoundException) {
            return null;
        } catch (JsonParsingException) {
            return null;
        }
    }

    public class VersionManifest {

        public Version launcherVersion { get; set; }
        public string resourcesCommitHash { get; set; }

    }

}