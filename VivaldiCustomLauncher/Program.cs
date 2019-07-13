using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

namespace VivaldiCustomLauncher {

    public static class Program {

        private static HttpClient _client;

        [STAThread]
        public static void Main() {
            Application.EnableVisualStyles();

            Application.ThreadException += (sender, args) => OnUncaughtException(args.Exception);
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => OnUncaughtException((Exception) args.ExceptionObject);

            try {
                string processToRun = Path.Combine(GetVivaldiApplicationDirectory(), "vivaldi.exe");

                string resourceDirectory = GetResourceDirectory(Path.GetDirectoryName(processToRun));

                ApplyTweaks(resourceDirectory);

                IEnumerable<string> originalArguments = Environment.GetCommandLineArgs().Skip(1);
                string processArgumentsToRun = CommandLine.ArgvToCommandLine(CustomizeArguments(originalArguments));

                CreateProcess(processToRun, processArgumentsToRun);
//            MessageBox.Show($"Started {processToRun} {processArgumentsToRun}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } catch (Exception e) {
                OnUncaughtException(e);
                throw;
            }
        }

        private static void OnUncaughtException(Exception e) {
            string message = (e as AggregateException)?.InnerException?.Message ?? e.Message;
            MessageBox.Show($"{e.GetType().Name}: {message}", "Failed to tweak and launch Vivaldi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void ApplyTweaks(string resourceDirectory) {
            string customStyleSheetRelativePath = Path.Combine("style", "custom.css");
            string customStyleSheetAbsolutePath = Path.Combine(resourceDirectory, customStyleSheetRelativePath);

            string customScriptRelativePath = Path.Combine("scripts", "custom.js");
            string customScriptAbsolutePath = Path.Combine(resourceDirectory, customScriptRelativePath);

            string bundleScriptAbsolutePath = Path.Combine(resourceDirectory, "bundle.js");
            string browserPageAbsolutePath = Path.Combine(resourceDirectory, "browser.html");

            Task.WaitAll(TweakBrowserHtml(browserPageAbsolutePath, customStyleSheetRelativePath, customScriptRelativePath),
                TweakCustomStyleSheet(customStyleSheetAbsolutePath),
                TweakBundleScript(bundleScriptAbsolutePath),
                TweakCustomScript(customScriptAbsolutePath));
        }

        private static async Task TweakBundleScript(string bundleScriptFile) {
            const string customizedComment = @"/* Customized by Ben */";
            char[] expectedHeader = customizedComment.ToCharArray();

            using (FileStream file = File.Open(bundleScriptFile, FileMode.Open, FileAccess.ReadWrite)) {
                string bundleContents;
                using (var reader = new StreamReader(file, Encoding.UTF8, false, 4 * 1024, true)) {
                    var buffer = new char[expectedHeader.Length];
                    await reader.ReadAsync(buffer, 0, buffer.Length);

                    if (expectedHeader.SequenceEqual(buffer)) {
                        return;
                    }

                    file.Seek(0, SeekOrigin.Begin);
                    reader.DiscardBufferedData();
                    bundleContents = await reader.ReadToEndAsync();

                    // Increase maximum tab width
                    bundleContents = Regex.Replace(bundleContents,
                        @"(?<prefix>TabStrip\.jsx.{1,1000}=)(?<minTabWidth>180)(?<suffix>,)",
                        match => match.Groups["prefix"].Value + 4000 + customizedComment + match.Groups["suffix"].Value);

                    // Remove extra spacing on the right side of tab bar
                    bundleContents = Regex.Replace(bundleContents,
                        @"(?<prefix>getStyle.{1,20}e=>.{1,200}this\.props\.maxWidth)(?<suffix>,)",
                        match => match.Groups["prefix"].Value + "+62" + customizedComment + match.Groups["suffix"].Value);
                }

                using (var writer = new StreamWriter(file, Encoding.UTF8)) {
                    file.Seek(0, SeekOrigin.Begin);
                    await writer.WriteAsync(expectedHeader);
                    await writer.WriteAsync(bundleContents);
                    await writer.FlushAsync();
                }
            }
        }

        private static async Task TweakCustomScript(string customScriptFile) {
            if (!File.Exists(customScriptFile)) {
                Directory.CreateDirectory(Path.GetDirectoryName(customScriptFile) ??
                                          throw new InvalidOperationException(
                                              "Could not get path of custom script file " + customScriptFile));

                using (FileStream fileStream = File.OpenWrite(customScriptFile))
                using (Stream downloadStream = await GetClient().GetStreamAsync(
                    @"https://gist.githubusercontent.com/Aldaviva/9fbe321331b7f80786a371e0fd4bcfaf/raw/%257C%2520scripts%255Ccustom.js")
                ) {
                    await downloadStream.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                }
            }
        }

        private static async Task TweakCustomStyleSheet(string customStyleSheetFile) {
            if (!File.Exists(customStyleSheetFile)) {
                using (FileStream fileStream = File.OpenWrite(customStyleSheetFile))
                using (Stream downloadStream = await GetClient().GetStreamAsync(
                    @"https://gist.githubusercontent.com/Aldaviva/9fbe321331b7f80786a371e0fd4bcfaf/raw/%257C%2520style%255Ccustom.css")
                ) {
                    await downloadStream.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                }
            }
        }

        private static async Task TweakBrowserHtml(string browserHtmlFile, string customStyleSheetRelativeFilePath,
            string customScriptRelativePath) {
            string fileContents = File.ReadAllText(browserHtmlFile, Encoding.UTF8);
            string styleSheetRelativeUri = new UriBuilder { Path = customStyleSheetRelativeFilePath }.Path;
            string scriptRelativeUri = new UriBuilder { Path = customScriptRelativePath }.Path;

            bool fileModified = false;
            string modifiedFileContents = fileContents;

            if (!fileContents.Contains(styleSheetRelativeUri)) {
                modifiedFileContents = modifiedFileContents
                    .Replace(@"  </head>", $"    <link rel=\"stylesheet\" href=\"{styleSheetRelativeUri}\" />\n  </head>");
                fileModified = true;
            }

            if (!fileContents.Contains(scriptRelativeUri)) {
                modifiedFileContents = modifiedFileContents.Replace(@"  </body>",
                    $"    <script src=\"{scriptRelativeUri}\"></script>\n  </body>");
                fileModified = true;
            }

            if (fileModified) {
                using (FileStream writeStream = File.OpenWrite(browserHtmlFile))
                using (var writer = new StreamWriter(writeStream, Encoding.UTF8)) {
                    await writer.WriteAsync(modifiedFileContents);
                    await writer.FlushAsync();
                }
            }
        }

        private static string GetVivaldiApplicationDirectory() {
            IEnumerable<(RegistryKey, string)> uninstallKeys = new[] {
                (Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Vivaldi"),
                (Registry.CurrentUser, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Vivaldi"),
                (Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Vivaldi"),
                (Registry.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Vivaldi")
            };

            foreach ((RegistryKey hive, string path) in uninstallKeys) {
                using (RegistryKey key = hive.OpenSubKey(path, false)) {
                    if (key != null) {
                        return (string) key.GetValue("InstallLocation");
                    }
                }
            }

            throw new InvalidOperationException("Could not find Vivaldi uninstallation key in registry");
        }

        private static string GetResourceDirectory(string applicationDirectory) {
            string versionDirectory = Directory.EnumerateDirectories(applicationDirectory)
                .Last(absoluteSubdirectory => {
                    string relativeSubdirectory = Path.GetFileName(absoluteSubdirectory) ??
                                                  throw new InvalidOperationException(
                                                      "No final directory component in path " + absoluteSubdirectory);
                    return Regex.IsMatch(relativeSubdirectory, @"\A\d+\.\d+\.\d+\.\d+\z");
                });

            return Path.Combine(versionDirectory, "resources", "vivaldi");
        }

        private static IEnumerable<string> CustomizeArguments(IEnumerable<string> originalArguments) {
            IList<string> customizedArguments = originalArguments.ToList();
            customizedArguments.Insert(0, "--force-renderer-accessibility");
            return customizedArguments;
        }

        private static int CreateProcess(string process, string arguments) {
            return Process.Start(process, arguments)?.Id ?? -1;
        }

        private static HttpClient GetClient() {
            return _client ?? (_client = new HttpClient());
        }

    }

}