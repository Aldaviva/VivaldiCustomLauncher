#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using VivaldiCustomLauncher.Tweaks;

namespace VivaldiCustomLauncher {

    public static class VivaldiLauncher {

        private static HttpClient? HTTP_CLIENT;
        private static HttpClient httpClient => HTTP_CLIENT ??= new HttpClient();

        [STAThread]
        public static void Main() {
            Application.EnableVisualStyles();

            Application.ThreadException                += (_, args) => onUncaughtException(args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (_, args) => onUncaughtException((Exception) args.ExceptionObject);

            tweakAndLaunch();
        }

        private static void tweakAndLaunch() {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try {
                string processToRun = Path.Combine(getVivaldiApplicationDirectory(), "vivaldi.exe");

                using Process? existingVivaldiProcess = Process.GetProcessesByName("vivaldi").FirstOrDefault();
                if (existingVivaldiProcess == null) {
                    string resourceDirectory = getResourceDirectory(Path.GetDirectoryName(processToRun)!);
                    applyTweaks(resourceDirectory);
                }

                IEnumerable<string> originalArguments     = Environment.GetCommandLineArgs().Skip(1);
                string              processArgumentsToRun = CommandLine.ArgvToCommandLine(customizeArguments(originalArguments));

                createProcess(processToRun, processArgumentsToRun);
                stopwatch.Stop();

                // MessageBox.Show($"Started {processToRun} {processArgumentsToRun} in {stopwatch.ElapsedMilliseconds:N0} ms", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch (Exception e) when (e is not OutOfMemoryException) {
                onUncaughtException(e);
                throw;
            }
        }

        private static void onUncaughtException(Exception e) {
            string message = (e as AggregateException)?.InnerException?.Message ?? e.Message;
            MessageBox.Show($"{e.GetType().Name}: {message}", "Failed to tweak and launch Vivaldi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void applyTweaks(string resourceDirectory) {
            string customStyleSheetRelativePath = Path.Combine("style", "custom.css");
            string customStyleSheetAbsolutePath = Path.Combine(resourceDirectory, customStyleSheetRelativePath);

            string customScriptRelativePath = Path.Combine("scripts", "custom.js");
            string customScriptAbsolutePath = Path.Combine(resourceDirectory, customScriptRelativePath);

            string bundleScriptAbsolutePath = Path.Combine(resourceDirectory, "bundle.js");
            string browserPageAbsolutePath  = Path.Combine(resourceDirectory, "browser.html");

            string visualElementsSourcePath      = Path.Combine(resourceDirectory, "../../..", "vivaldi.VisualElementsManifest.xml");
            string visualElementsDestinationPath = Path.Combine(resourceDirectory, "../../../..", Assembly.GetExecutingAssembly().GetName().Name + ".VisualElementsManifest.xml");

            Task.WaitAll(
                applyTweakIfNecessary(new BrowserHtmlTweak(), new BrowserHtmlTweakParams(browserPageAbsolutePath, customStyleSheetRelativePath, customScriptRelativePath)),
                applyTweakIfNecessary(new CustomStyleSheetTweak(httpClient), new BaseTweakParams(customStyleSheetAbsolutePath)),
                applyTweakIfNecessary(new BundleScriptTweak(), new BaseTweakParams(bundleScriptAbsolutePath)),
                applyTweakIfNecessary(new CustomScriptTweak(httpClient), new BaseTweakParams(customScriptAbsolutePath)),
                applyTweakIfNecessary(new VisualElementsManifestTweak(), new VisualElementsManifestTweakParams(visualElementsSourcePath, visualElementsDestinationPath)));
        }

        private static async Task applyTweakIfNecessary<OutputType, Params>(Tweak<OutputType, Params> tweak, Params tweakParams) where Params: TweakParams where OutputType: class {
            OutputType? editedFile = await tweak.readFileAndEditIfNecessary(tweakParams);
            if (editedFile != null) {
                await tweak.saveFile(editedFile, tweakParams);
            }
        }

        private static string getVivaldiApplicationDirectory() {
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
                .OrderByDescending(Path.GetFileName, new VersionNumberComparer())
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
            return Process.Start(process, arguments)?.Id ?? -1;
        }

    }

}