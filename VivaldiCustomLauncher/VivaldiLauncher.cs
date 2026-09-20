#nullable enable

using Bom.Squad;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Win32;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Unfucked.HTTP;
using Unfucked.Windows;
using VivaldiCustomLauncher.Tweaks;
using Windows.Win32.System.Threading;

namespace VivaldiCustomLauncher;

public static class VivaldiLauncher {

    public static readonly AssemblyName CURRENT_ASSEMBLY = Assembly.GetExecutingAssembly().GetName();

    private static Lazy<HttpClient> httpClient => new(() => new UnfuckedHttpClient(new HttpClientHandler {
        MaxConnectionsPerServer = 24,
        AllowAutoRedirect       = true,
        AutomaticDecompression  = DecompressionMethods.GZip
    }) {
        Timeout               = TimeSpan.FromSeconds(10),
        DefaultRequestHeaders = { UserAgent = { new ProductInfoHeaderValue(CURRENT_ASSEMBLY.Name, CURRENT_ASSEMBLY.Version.ToString()) } }
    }, LazyThreadSafetyMode.PublicationOnly);

    [STAThread]
    public static async Task<int> Main() {
        Application.ThreadException                += (_, args) => onUncaughtException(args.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, args) => onUncaughtException((Exception) args.ExceptionObject);

        Application.EnableVisualStyles();
        Version.PrintProgramVersionAndExitIfRequested();
        BomSquad.DefuseUtf8Bom();

        try {
            return await tweakAndLaunch() ? Environment.ExitCode : 1;
        } finally {
            // ReSharper disable once MethodHasAsyncOverload - no it doesn't, HttpClient isn't async disposable
            httpClient.TryDisposeValue();
        }
    }

    private static async Task<bool> tweakAndLaunch() {
        Stopwatch              stopwatch      = Stopwatch.StartNew();
        bool                   success        = true;
        HashSet<CachedProcess> setupProcesses = [];
        Semaphore?             instanceLock   = null; // use Semaphore because Mutex crashes if released on a different thread than it was acquired on, which usually happens with async

        try {
            (CommandLine.Arguments arguments, CommandLineApplication<CommandLine.Arguments> argsParser) = CommandLine.parse();
            if (argsParser.IsShowingInformation) {
                return true;
            }

            if (arguments.installUpgradeInterceptor) {
                try {
                    using RegistryKey imageFileExecutionOptions = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options", true)!;
                    using RegistryKey updateNotifierKey         = imageFileExecutionOptions.CreateSubKey("update_notifier.exe", true);
                    using Process     currentProcess            = Process.GetCurrentProcess();
                    updateNotifierKey.SetValue("Debugger", $"\"{currentProcess.MainModule!.FileName}\" --intercept-update-notifier");
                    MessageBox.Show("Installed upgrade interceptor.", CURRENT_ASSEMBLY.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return true;
                } catch (Exception e) when (e is SecurityException or UnauthorizedAccessException) {
                    MessageBox.Show("Failed to write to local machine registry. Make sure to run this program elevated (as administrator).", CURRENT_ASSEMBLY.Name, MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return false;
                }
            }

            GitHubClient    gitHubClient            = new(httpClient.Value);
            ProgramUpgrader programUpgrader         = new(gitHubClient);
            bool            isBlockingVivaldiUpdate = arguments.interceptUpdateNotifier.active;

            if (isBlockingVivaldiUpdate) {
                Process updateNotifier;
                if (arguments.interceptUpdateNotifier.pid is {} updateNotifierPid) {
                    Console.WriteLine("This program restarted while upgrading itself");
                    try {
                        updateNotifier = Process.GetProcessById(updateNotifierPid);
                    } catch (ArgumentException) {
                        return false;
                    }
                } else {
                    Console.WriteLine("Intercepted execution of update_notifier using Image File Execution Options");
                    // IList<string> realUpdateNotifierCommands    = Environment.GetCommandLineArgs().Skip(2).ToList();
                    Span<char> realUpdateNotifierCommandLine = (Process.CommandLineToString(arguments.extras) + '\0').ToCharArray().AsSpan();
                    unsafe {
                        Console.WriteLine($"Launching real {realUpdateNotifierCommandLine.ToString()}");
                        if (CreateProcess(arguments.extras[0], ref realUpdateNotifierCommandLine, bInheritHandles: false,
                                dwCreationFlags: PROCESS_CREATION_FLAGS.DEBUG_ONLY_THIS_PROCESS, lpStartupInfo: new STARTUPINFOW(), lpProcessInformation: out PROCESS_INFORMATION processInfo)) {
                            try {
                                updateNotifier = Process.GetProcessById((int) processInfo.dwProcessId);
                            } catch (ArgumentException e) {
                                MessageBox.Show($"Real update_notifier:{processInfo.dwProcessId} exited immediately (error={e.Message}, cmdline={realUpdateNotifierCommandLine.ToString()})",
                                    CURRENT_ASSEMBLY.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                            DebugActiveProcessStop(processInfo.dwProcessId);
                        } else {
                            MessageBox.Show($"0x{Marshal.GetLastWin32Error():x}: Failed to start {arguments.extras[0]} with command line {realUpdateNotifierCommandLine.ToString()}",
                                CURRENT_ASSEMBLY.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                }

                using (updateNotifier) {
                    if (arguments.extras.Contains("--is-enabled") || arguments.extras.Contains("--enable") || arguments.extras.Contains("--disable") ||
                        arguments.extras.Contains("--browser-startup")) {
                        try {
                            updateNotifier.EnableRaisingEvents = true; // undocumented: required to read ExitCode when process was not launched with Process.Start
                            updateNotifier.WaitForExit();
                        } catch (InvalidOperationException) {
                            // updateNotifier already exited
                        }
                        Environment.ExitCode = updateNotifier.ExitCode;
                        return true;
                    } else if (!arguments.interceptUpdateNotifier.pid.HasValue && await programUpgrader.upgrade(updateNotifier.Id)) {
                        return true;
                    }

                    Stopwatch sinceUpdateNotifierExited = new();
                    Console.WriteLine("Waiting for user to start upgrade");
                    for (bool first = true; setupProcesses.Count == 0; first = false) {
                        if (updateNotifier.HasExited) {
                            sinceUpdateNotifierExited.Start();
                            if (sinceUpdateNotifierExited.Elapsed > TimeSpan.FromSeconds(20)) {
                                return true;
                            }
                        }
                        if (!first) {
                            await Task.Delay(2000);
                        }
                        populateSetups();
                    }

                    Console.WriteLine("update_notifier launched setup");
                    instanceLock = new Semaphore(1, 1, CURRENT_ASSEMBLY.Name);
                    if (!instanceLock.WaitOne(0)) {
                        Console.WriteLine("Another instance of VivaldiCustomLauncher is already intercepting setup, exiting");
                        instanceLock.Dispose();
                        instanceLock = null;
                        return true;
                    }

                    using RegistryKey vivaldiUninstallKey       = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Vivaldi", false)!;
                    string            oldVersion                = getInstalledVersionFromRegistry();
                    string            newVersion                = oldVersion;
                    TimeSpan          versionCheckInterval      = TimeSpan.FromMilliseconds(25);
                    TimeSpan          setupRepopulationInterval = TimeSpan.FromSeconds(0.5);

                    for (int versionChecks = 1; newVersion.Equals(oldVersion, StringComparison.Ordinal); versionChecks++) {
                        await Task.Delay(versionCheckInterval);
                        if (versionChecks % (int) (setupRepopulationInterval.TotalMilliseconds / versionCheckInterval.TotalMilliseconds) == 0) {
                            populateSetups();
                        }
                        newVersion = getInstalledVersionFromRegistry();
                    }

                    Console.WriteLine("setup installed Vivaldi and is about to launch the new version");
                    foreach (CachedProcess setup in setupProcesses) {
                        try {
                            setup.process.Suspended = true;
                            Console.WriteLine("Suspended setup.exe:" + setup.pid);
                        } catch (InvalidOperationException) {} // process already exited, continue
                    }
                    HashSet<CachedProcess> previousSetupProcesses = [.. setupProcesses];
                    populateSetups();
                    foreach (CachedProcess setup in setupProcesses.Except(previousSetupProcesses)) {
                        try {
                            setup.process.Suspended = true;
                            Console.WriteLine("Suspended setup.exe:" + setup.pid);
                        } catch (InvalidOperationException) {} // process already exited, continue
                    }

                    void populateSetups() {
                        foreach (Process process in Process.GetProcessesByName("setup")) {
                            try {
                                CachedProcess cachedProcess = new(process);
                                if (!setupProcesses.Contains(cachedProcess) && process.MainModule?.FileVersionInfo.ProductName?.Trim() == "Vivaldi Installer") {
                                    setupProcesses.Add(cachedProcess);
                                } else {
                                    process.Dispose();
                                }
                            } catch (Exception e) when (e is Win32Exception or InvalidOperationException) {
                                process.Dispose(); // race condition, process probably exited already
                            }
                        }
                    }

                    string getInstalledVersionFromRegistry() =>
                        (string) vivaldiUninstallKey.GetValue("DisplayVersion", string.Empty);
                }
            }

            string vivaldiApplicationDirectory = getVivaldiApplicationDirectory(arguments.vivaldiApplicationDirectory);
            string processToRun                = Path.Combine(vivaldiApplicationDirectory, "vivaldi.exe");

            using Process? existingVivaldiProcess = Process.GetProcessesByName("vivaldi").FirstOrDefault();
            if (existingVivaldiProcess == null || isBlockingVivaldiUpdate) {
                Task<bool>    isInstallationPendingTask   = !isBlockingVivaldiUpdate ? programUpgrader.upgrade() : Task.FromResult(false);
                Task<string?> resourcesRepoCommitHashTask = gitHubClient.fetchLatestCommitHash("Aldaviva", "VivaldiCustomResources");
                (string resourceDirectory, Version browserVersion) = getResourceDirectory(Path.GetDirectoryName(processToRun)!);
                string                 tweakManifestAbsolutePath = Path.GetFullPath(Path.Combine(resourceDirectory, @"..\..\..\..", CURRENT_ASSEMBLY.Name + "-manifest.json"));
                Task<VersionManifest?> versionManifestTask       = readVersionManifest(tweakManifestAbsolutePath);

                if (await isInstallationPendingTask) {
                    Console.WriteLine("Upgrading {0} to a new version", CURRENT_ASSEMBLY.Name);
                    return true;
                }

                string? resourcesRepoCommitHash = await resourcesRepoCommitHashTask;
                if (resourcesRepoCommitHash != null) {
                    Console.WriteLine($"Latest resources repo commit hash is {resourcesRepoCommitHash}");
                    TweakedFiles tweakedFiles = new(resourceDirectory);

                    VersionManifest? versionManifest    = await versionManifestTask;
                    bool             browserWasUpgraded = (versionManifest != null && browserVersion != versionManifest.browserVersion) || isBlockingVivaldiUpdate;
                    bool shouldApplyTweaks = versionManifest == null
                        || resourcesRepoCommitHash != versionManifest.resourcesCommitHash
                        || CURRENT_ASSEMBLY.Version != versionManifest.launcherVersion
                        || browserWasUpgraded; // tweaks are from different launcher or resources, or the browser was updated
                    bool wasAlreadyTweaked = shouldApplyTweaks && !isBlockingVivaldiUpdate
                        && (versionManifest != null || File.Exists(Path.Combine(resourceDirectory, tweakedFiles.relative.customScript)));
                    bool shouldUntweak = (wasAlreadyTweaked && !browserWasUpgraded) || arguments.untweak;

                    if (shouldUntweak) {
                        // Revert existing tweaks because they are outdated, or just upgraded to first launcher version that uses manifest files
                        Console.WriteLine("Unapplying tweaks");
                        string installerFile = Path.GetFullPath(Path.Combine(resourceDirectory, @"..\..\Installer\vivaldi.7z"));
                        unapplyTweaks(vivaldiApplicationDirectory, installerFile, tweakedFiles);
                        File.Delete(tweakManifestAbsolutePath);
                    }

                    if (shouldApplyTweaks) {
                        try {
                            Console.WriteLine("Applying tweaks");
                            await applyTweaks(tweakedFiles);

                            Console.WriteLine($"Writing manifest file to {tweakManifestAbsolutePath}");
                            using FileStream manifestWriteStream = new(tweakManifestAbsolutePath, FileMode.Create, FileAccess.Write, FileShare.None);
                            VersionManifest  newManifest         = new(CURRENT_ASSEMBLY.Version, resourcesRepoCommitHash, browserVersion);
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

            if (!arguments.noVivaldiLaunch && !isBlockingVivaldiUpdate) {
                try {
                    IEnumerable<string> originalArguments     = arguments.extras;
                    string              processArgumentsToRun = Process.CommandLineToString(customizeArguments(originalArguments));
                    createProcess(processToRun, processArgumentsToRun);
                } catch (InvalidOperationException e) {
                    MessageBox.Show(e.Message, "Failed to launch Vivaldi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    success = false;
                }
            }

            foreach (CachedProcess setup in setupProcesses) {
                try {
                    setup.process.Suspended = false;
                    Console.WriteLine("Resumed setup.exe:" + setup.pid);
                } catch (Exception e) when (e is not OutOfMemoryException) {
                    MessageBox.Show($"Failed to resume setup.exe:{setup.pid}, skipping it.\n{e.GetType().Name}: {e.Message}");
                }
            }

            File.Delete(Path.Combine(vivaldiApplicationDirectory, "VivaldiCustomLauncher.manifest.json")); // old 1.3.0 file location, not used any more

            stopwatch.Stop();

        } catch (Exception e) when (e is not OutOfMemoryException) {
            onUncaughtException(e);
            success = false;
        } finally {
            foreach (CachedProcess setup in setupProcesses) {
                setup.Dispose();
            }
            instanceLock?.Release();
            instanceLock?.Dispose();
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
                applyTweak(new CustomStyleSheetTweak(httpClient.Value), new BaseTweakParams(files.customStyleSheet)),
                applyTweak(new ModStyleSheetTweak(httpClient.Value), new BaseTweakParams(files.modStyleSheet)),
                applyTweak(new BundleScriptTweak(), new BaseTweakParams(files.bundleScript)),
                applyTweak(new BackgroundBundleScriptTweak(), new BaseTweakParams(files.backgroundBundleScript)),
                applyTweak(new CustomScriptTweak(httpClient.Value), new BaseTweakParams(files.customScript)),
                applyTweak(new VisualElementsManifestTweak(), new VisualElementsManifestTweakParams(files.visualElementsSource, files.visualElementsDestination)),
                applyTweak(new ShowFeedHtmlTweak(), new ShowFeedHtmlTweakParams(files.showFeedPage, files.relative.customFeedScript)),
                applyTweak(new CustomFeedScriptTweak(httpClient.Value), new BaseTweakParams(files.customFeedScript))
            );
        } catch (AggregateException e) {
            if (e.InnerExceptions.OfType<TweakException>().FirstOrDefault() is {} tweakException) {
                throw tweakException;
            } else {
                throw e.InnerException!;
            }
        }
    }

    /// <exception cref="TweakException"></exception>
    private static async Task applyTweak<OUTPUTTYPE, PARAMS>(Tweak<OUTPUTTYPE, PARAMS> tweak, PARAMS tweakParams) where PARAMS: TweakParams where OUTPUTTYPE: class {
        OUTPUTTYPE editedFile = await tweak.readAndEditFile(tweakParams);
        await tweak.saveFile(editedFile, tweakParams);
        Console.WriteLine($"Tweaked {tweakParams.filename}");
    }

    private static void unapplyTweaks(string vivaldiApplicationDirectory, string installerArchiveAbsolutePath, TweakedFiles files) {
        using IArchive installerArchive = SevenZipArchive.OpenArchive(installerArchiveAbsolutePath, new ReaderOptions { DisableCheckIncomplete = true }); // takes about 1 second

        foreach (string fileToRestore in files.overwrittenFiles) {
            string         filenameInArchive = "Vivaldi-bin/" + fileToRestore.Remove(0, vivaldiApplicationDirectory.Length).Replace('\\', '/').TrimStart('/');
            IArchiveEntry? fileInArchive     = installerArchive.Entries.FirstOrDefault(entry => string.Equals(entry.Key, filenameInArchive, StringComparison.OrdinalIgnoreCase));

            if (fileInArchive != null) {
                Console.WriteLine($"Extracting {filenameInArchive} to {fileToRestore}");
                Directory.CreateDirectory(Path.GetDirectoryName(fileToRestore)!);
                fileInArchive.WriteToFile(fileToRestore, new ExtractionOptions { Overwrite = true, PreserveAttributes = true });
            } else {
                Console.WriteLine($"Could not find {filenameInArchive} in {installerArchiveAbsolutePath}");
            }
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
            IEnumerable<(RegistryKey hive, string path)> uninstallKeys = [
                (Registry.CurrentUser, UNINSTALL_REGISTRY_PATH),
                (Registry.CurrentUser, UNINSTALL_REGISTRY_PATH_WOW64),
                (Registry.LocalMachine, UNINSTALL_REGISTRY_PATH),
                (Registry.LocalMachine, UNINSTALL_REGISTRY_PATH_WOW64)
            ];

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

    private static (string directory, Version browserVersion) getResourceDirectory(string applicationDirectory) {
        return Directory.EnumerateDirectories(applicationDirectory)
            .Where(absoluteSubdirectory => {
                string relativeSubdirectory = Path.GetFileName(absoluteSubdirectory)!;
                return Regex.IsMatch(relativeSubdirectory, @"\A(?:\d+\.){3}\d+\z");
            })
            .Select(absoluteSubdirectory => (directory: Path.Combine(absoluteSubdirectory, "resources", "vivaldi"), version: Version.Parse(Path.GetFileName(absoluteSubdirectory))))
            .OrderByDescending(versionedDirectory => versionedDirectory.version)
            .First();
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
        } catch (JsonException) {
            return null;
        }
    }

    /// <exception cref="InvalidOperationException"><paramref name="process"/> already exited</exception>
    /// <exception cref="Win32Exception"><paramref name="process"/> already exited</exception>
    private readonly record struct CachedProcess(Process process): IDisposable {

        public readonly  Process  process   = process;
        public readonly  int      pid       = process.Id;
        private readonly DateTime startTime = process.StartTime;

        public bool Equals(CachedProcess other) => pid == other.pid && startTime.Equals(other.startTime);

        public override int GetHashCode() {
            unchecked {
                return (pid * 397) ^ startTime.GetHashCode();
            }
        }

        public void Dispose() {
            process.Dispose();
        }

    }

}