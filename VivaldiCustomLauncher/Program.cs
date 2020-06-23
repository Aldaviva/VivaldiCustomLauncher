#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VivaldiCustomLauncher {

    public static class Program {

        private static HttpClient? HTTP_CLIENT;
        private static HttpClient httpClient => HTTP_CLIENT ??= new HttpClient();

        [STAThread]
        public static void Main() {
            var stopwatch = Stopwatch.StartNew();

            Application.EnableVisualStyles();

            Application.ThreadException += (sender, args) => onUncaughtException(args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => onUncaughtException((Exception) args.ExceptionObject);

            try {
                string processToRun = Path.Combine(getVivaldiApplicationDirectory(), "vivaldi.exe");

                using Process existingVivaldiProcess = Process.GetProcessesByName("vivaldi").FirstOrDefault();
                if (existingVivaldiProcess == null) {
                    string resourceDirectory = getResourceDirectory(Path.GetDirectoryName(processToRun));
                    applyTweaks(resourceDirectory);
                }

                IEnumerable<string> originalArguments = Environment.GetCommandLineArgs().Skip(1);
                string processArgumentsToRun = CommandLine.ArgvToCommandLine(customizeArguments(originalArguments));

                createProcess(processToRun, processArgumentsToRun);
                stopwatch.Stop();

                // MessageBox.Show($"Started {processToRun} {processArgumentsToRun} in {stopwatch.ElapsedMilliseconds:N0} ms", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch (Exception e) when (!(e is OutOfMemoryException)) {
                onUncaughtException(e);
                throw;
            }
        }

        private static void onUncaughtException(Exception e) {
            string message = (e as AggregateException)?.InnerException?.Message ?? e.Message;
            MessageBox.Show($"{e.GetType().Name}: {message}", "Failed to tweak and launch Vivaldi", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void applyTweaks(string resourceDirectory) {
            string customStyleSheetRelativePath = Path.Combine("style", "custom.css");
            string customStyleSheetAbsolutePath = Path.Combine(resourceDirectory, customStyleSheetRelativePath);

            string customScriptRelativePath = Path.Combine("scripts", "custom.js");
            string customScriptAbsolutePath = Path.Combine(resourceDirectory, customScriptRelativePath);

            string bundleScriptAbsolutePath = Path.Combine(resourceDirectory, "bundle.js");
            string browserPageAbsolutePath = Path.Combine(resourceDirectory, "browser.html");

            string visualElementsSourcePath = Path.Combine(resourceDirectory, "../../..", "vivaldi.VisualElementsManifest.xml");
            string visualElementsDestinationPath = Path.Combine(resourceDirectory, "../../../..",
                Assembly.GetExecutingAssembly().GetName().Name + ".VisualElementsManifest.xml");

            Task.WaitAll(tweakBrowserHtml(browserPageAbsolutePath, customStyleSheetRelativePath, customScriptRelativePath),
                tweakCustomStyleSheet(customStyleSheetAbsolutePath),
                tweakBundleScript(bundleScriptAbsolutePath),
                tweakCustomScript(customScriptAbsolutePath),
                tweakVisualElementsManifest(visualElementsSourcePath, visualElementsDestinationPath));
        }

        private static async Task tweakBundleScript(string bundleScriptFile) {
            const string CUSTOMIZED_COMMENT = @"/* Customized by Ben */";
            char[] expectedHeader = CUSTOMIZED_COMMENT.ToCharArray();
            string bundleContents;
            using FileStream file = File.Open(bundleScriptFile, FileMode.Open, FileAccess.ReadWrite);

            using (var reader = new StreamReader(file, Encoding.UTF8, false, 4 * 1024, true)) {
                var buffer = new char[expectedHeader.Length];
                await reader.ReadAsync(buffer, 0, buffer.Length);

                if (expectedHeader.SequenceEqual(buffer)) {
                    return;
                }

                file.Seek(0, SeekOrigin.Begin);
                reader.DiscardBufferedData();
                bundleContents = await reader.ReadToEndAsync();
            }

            // Increase maximum tab width
            bundleContents = Regex.Replace(bundleContents,
                @"(?<prefix>\bmaxWidth=)(?<minTabWidth>180)(?<suffix>,)",
                match => match.Groups["prefix"].Value + 4000 + CUSTOMIZED_COMMENT + match.Groups["suffix"].Value);

            // Remove extra spacing on the right side of tab bar
            bundleContents = Regex.Replace(bundleContents,
                @"(?<prefix>\bgetStyle.{1,20}e=>.{1,200}this\.props\.maxWidth)(?<suffix>,)",
                match => match.Groups["prefix"].Value + "+62" + CUSTOMIZED_COMMENT + match.Groups["suffix"].Value);

            // Make Back also close the tab if the page can't go back
            bundleContents = Regex.Replace(bundleContents,
                @"(?<prefix>{name:""COMMAND_PAGE_BACK"",action:)re\.a\.back(?<suffix>,)",
                match => match.Groups["prefix"].Value +
                         @"()=>{const c=p.a.getActivePage(),e=c&&a(94).a.getNavigationInfo(c.id);e&&e.canGoBack?re.a.back():m.a.close()}" +
                         CUSTOMIZED_COMMENT + match.Groups["suffix"].Value);

            using var writer = new StreamWriter(file, Encoding.UTF8);
            file.Seek(0, SeekOrigin.Begin);
            await writer.WriteAsync(expectedHeader);
            await writer.WriteAsync(bundleContents);
            await writer.FlushAsync();
        }

        private static async Task tweakCustomScript(string customScriptFile) {
            if (!File.Exists(customScriptFile)) {
                Directory.CreateDirectory(Path.GetDirectoryName(customScriptFile) ??
                                          throw new InvalidOperationException(
                                              "Could not get path of custom script file " + customScriptFile));

                using FileStream fileStream = File.OpenWrite(customScriptFile);
                using Stream downloadStream = await httpClient.GetStreamAsync(
                    @"https://gist.githubusercontent.com/Aldaviva/9fbe321331b7f80786a371e0fd4bcfaf/raw/%257C%2520scripts%255Ccustom.js");
                await downloadStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }
        }

        private static async Task tweakCustomStyleSheet(string customStyleSheetFile) {
            if (!File.Exists(customStyleSheetFile)) {
                using FileStream fileStream = File.OpenWrite(customStyleSheetFile);
                using Stream downloadStream = await httpClient.GetStreamAsync(
                    @"https://gist.githubusercontent.com/Aldaviva/9fbe321331b7f80786a371e0fd4bcfaf/raw/%257C%2520style%255Ccustom.css");
                await downloadStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }
        }

        private static async Task tweakBrowserHtml(string browserHtmlFile, string customStyleSheetRelativeFilePath,
            string customScriptRelativePath) {
            string fileContents = File.ReadAllText(browserHtmlFile, Encoding.UTF8);
            string styleSheetRelativeUri = new UriBuilder { Path = customStyleSheetRelativeFilePath }.Path;
            string scriptRelativeUri = new UriBuilder { Path = customScriptRelativePath }.Path;

            bool fileModified = false;
            string modifiedFileContents = fileContents;

            if (!fileContents.Contains(styleSheetRelativeUri)) {
                modifiedFileContents = modifiedFileContents.Replace(@"  </head>",
                    $"    <link rel=\"stylesheet\" href=\"{styleSheetRelativeUri}\" />\n  </head>");
                fileModified = true;
            }

            if (!fileContents.Contains(scriptRelativeUri)) {
                modifiedFileContents = modifiedFileContents.Replace(@"  </body>",
                    $"    <script src=\"{scriptRelativeUri}\"></script>\n  </body>");
                fileModified = true;
            }

            if (fileModified) {
                using FileStream writeStream = File.OpenWrite(browserHtmlFile);
                using var writer = new StreamWriter(writeStream, Encoding.UTF8);
                await writer.WriteAsync(modifiedFileContents);
                await writer.FlushAsync();
            }
        }

        private static async Task tweakVisualElementsManifest(string visualElementsSourcePath, string visualElementsDestinationPath) {
            var visualElementsManifestEditor = new VisualElementsManifestEditor();

            Task<ApplicationManifest> loadSourceTask = Task.Run(() => {
                ApplicationManifest sourceManifest = visualElementsManifestEditor.load(visualElementsSourcePath);
                visualElementsManifestEditor.relativizeUris(sourceManifest, "Application");
                return sourceManifest;
            });

            Task<ApplicationManifest?> loadDestinationTask = Task.Run(() => {
                try {
                    return visualElementsManifestEditor.load(visualElementsDestinationPath);
                } catch (FileNotFoundException) {
                    return null;
                }
            });

            ApplicationManifest source = await loadSourceTask;
            ApplicationManifest? destination = await loadDestinationTask;

            if (!destination?.Equals(source) ?? true) {
                visualElementsManifestEditor.save(source, visualElementsDestinationPath);
            }
        }

        private static string getVivaldiApplicationDirectory() {
            IEnumerable<(RegistryKey, string)> uninstallKeys = new[] {
                (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Vivaldi"),
                (Registry.CurrentUser, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Vivaldi"),
                (Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Vivaldi"),
                (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Vivaldi")
            };

            foreach ((RegistryKey hive, string path) in uninstallKeys) {
                using RegistryKey key = hive.OpenSubKey(path, false);
                if (key != null) {
                    return (string) key.GetValue("InstallLocation");
                }
            }

            throw new InvalidOperationException("Could not find Vivaldi uninstallation key in registry");
        }

        private static string getResourceDirectory(string applicationDirectory) {
            string versionDirectory = Directory.EnumerateDirectories(applicationDirectory)
                .Where(absoluteSubdirectory => {
                    string relativeSubdirectory = Path.GetFileName(absoluteSubdirectory) ?? throw new InvalidOperationException(
                        "No final directory component in path " + applicationDirectory);
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