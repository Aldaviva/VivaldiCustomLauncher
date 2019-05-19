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

namespace VivaldiCustomLauncher
{
    public static class Program
    {
        private static HttpClient _client;

        [STAThread]
        public static void Main()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (Environment.GetCommandLineArgs().Length == 1)
            {
                Application.Run(new ConfigurationDialog());
            }
            else
            {
                string processToRun = Environment.GetCommandLineArgs()[1];

                string resourceDirectory = GetResourceDirectory(Path.GetDirectoryName(processToRun));

                ApplyTweaks(resourceDirectory);

                //make non-debugged copy of executable so we don't get into an infinite loop of launching the real Vivaldi, only to have it start this launcher again
                string processDirectory = Path.GetDirectoryName(processToRun) ??
                                          throw new InvalidOperationException(
                                              "Could not get directory name of path " + processToRun);
                string renamedProcess = Path.Combine(processDirectory,
                    Path.GetFileNameWithoutExtension(processToRun) + "2" + Path.GetExtension(processToRun));
                if (IsBrowserExecutableDifferent(processToRun, renamedProcess))
                {
                    try
                    {
                        File.Copy(processToRun, renamedProcess, true);
                    }
                    catch (IOException)
                    {
                        //failed to copy browser to a non-debugged copy, so ignore.
                        //can be caused by the browser already running when this program runs, so there is already a write lock on the executable
                    }
                }

                IEnumerable<string> originalArguments = Environment.GetCommandLineArgs().Skip(2);
                string processArgumentsToRun = CommandLine.ArgvToCommandLine(CustomizeArguments(originalArguments));

                CreateProcess(renamedProcess, processArgumentsToRun);
                stopwatch.Stop();
//                MessageBox.Show($"Launched Vivaldi in {stopwatch.ElapsedMilliseconds} ms.", "VivaldiCustomLauncher", MessageBoxButtons.OK, MessageBoxIcon.Information);

//                MessageBox.Show($"Started {renamedProcess} {processArgumentsToRun}\n\nPID: {pid}", "Success", MessageBoxButtons.OK,
//                    MessageBoxIcon.Information);
            }
        }

        private static void ApplyTweaks(string resourceDirectory)
        {
            string customStyleSheetRelativePath = Path.Combine("style", "custom.css");
            string customStylSheetAbsolutePath = Path.Combine(resourceDirectory, customStyleSheetRelativePath);

            string customScriptRelativePath = Path.Combine("scripts", "custom.js");
            string customScriptAbsolutePath = Path.Combine(resourceDirectory, customScriptRelativePath);

            Task.WaitAll(
                TweakBrowserHtml(Path.Combine(resourceDirectory, "browser.html"), customStyleSheetRelativePath,
                    customScriptRelativePath),
                TweakCustomStyleSheet(customStylSheetAbsolutePath),
                TweakBundleScript(Path.Combine(resourceDirectory, "bundle.js")),
                TweakCustomScript(customScriptAbsolutePath));
        }

        private static async Task TweakBundleScript(string bundleScriptFile)
        {
            const string customizedComment = @"/* Customized by Ben */";
            char[] expectedHeader = customizedComment.ToCharArray();

            using (FileStream file = File.Open(bundleScriptFile, FileMode.Open, FileAccess.ReadWrite))
            {
                string bundleContents;
                using (var reader = new StreamReader(file, Encoding.UTF8, false, 4 * 1024, true))
                {
                    var buffer = new char[expectedHeader.Length];
                    await reader.ReadAsync(buffer, 0, buffer.Length);

                    if (expectedHeader.SequenceEqual(buffer))
                    {
                        return;
                    }

                    file.Seek(0, SeekOrigin.Begin);
                    reader.DiscardBufferedData();
                    bundleContents = await reader.ReadToEndAsync();

                    // Increase maximum tab width
                    bundleContents = Regex.Replace(bundleContents, @"(?<prefix>TabStrip\.jsx.{1,1000}?=)(?<minTabWidth>180)(?<suffix>,)",
                        match => match.Groups["prefix"].Value + 2000 + customizedComment + match.Groups["suffix"].Value);

                    // Remove extra spacing on the right side of tab bar
                    bundleContents = Regex.Replace(bundleContents,
                        @"(?<prefix>getStyles"",e=>.{1,200}?this\.props\.maxWidth)(?<suffix>,)",
                        match => match.Groups["prefix"].Value + @"+62" + customizedComment + match.Groups["suffix"].Value);
                }

                using (var writer = new StreamWriter(file, Encoding.UTF8))
                {
                    file.Seek(0, SeekOrigin.Begin);
                    await writer.WriteAsync(expectedHeader);
                    await writer.WriteAsync(bundleContents);
                    await writer.FlushAsync();
                }
            }
        }


        private static async Task TweakCustomScript(string customScriptFile)
        {
            if (!File.Exists(customScriptFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(customScriptFile) ??
                                          throw new InvalidOperationException(
                                              "Could not get path of custom script file " + customScriptFile));

                using (FileStream fileStream = File.OpenWrite(customScriptFile))
                using (Stream downloadStream = await GetClient().GetStreamAsync(
                    @"https://gist.githubusercontent.com/Aldaviva/9fbe321331b7f80786a371e0fd4bcfaf/raw/%257C%2520scripts%255Ccustom.js")
                )
                {
                    await downloadStream.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                }
            }
        }


        private static async Task TweakCustomStyleSheet(string customStylSheetFile)
        {
            if (!File.Exists(customStylSheetFile))
            {
                using (FileStream fileStream = File.OpenWrite(customStylSheetFile))
                using (Stream downloadStream = await GetClient().GetStreamAsync(
                    @"https://gist.githubusercontent.com/Aldaviva/9fbe321331b7f80786a371e0fd4bcfaf/raw/%257C%2520style%255Ccustom.css")
                )
                {
                    await downloadStream.CopyToAsync(fileStream);
                    await fileStream.FlushAsync();
                }
            }
        }

        private static async Task TweakBrowserHtml(string browserHtmlFile, string customStyleSheetRelativeFilePath,
            string customScriptRelativePath)
        {
            string fileContents = File.ReadAllText(browserHtmlFile, Encoding.UTF8);
            string styleSheetRelativeUri = new UriBuilder { Path = customStyleSheetRelativeFilePath }.Path;
            string scriptRelativeUri = new UriBuilder { Path = customScriptRelativePath }.Path;

            bool fileModified = false;
            string modifiedFileContents = fileContents;

            if (!fileContents.Contains(styleSheetRelativeUri))
            {
                modifiedFileContents = modifiedFileContents
                    .Replace(@"  </head>", $"    <link rel=\"stylesheet\" href=\"{styleSheetRelativeUri}\" />\n  </head>");
                fileModified = true;
            }

            if (!fileContents.Contains(scriptRelativeUri))
            {
                modifiedFileContents = modifiedFileContents.Replace(@"  </body>",
                    $"    <script src=\"{scriptRelativeUri}\"></script>\n  </body>");
                fileModified = true;
            }

            if (fileModified)
            {
                using (FileStream writeStream = File.OpenWrite(browserHtmlFile))
                {
                    using (var writer = new StreamWriter(writeStream, Encoding.UTF8))
                    {
                        await writer.WriteAsync(modifiedFileContents);
                        await writer.FlushAsync();
                    }
                }
            }
        }

        private static string GetResourceDirectory(string applicationDirectory)
        {
            string versionDirectory = Directory.EnumerateDirectories(applicationDirectory)
                .Last(absoluteSubdirectory =>
                {
                    string relativeSubdirectory = Path.GetFileName(absoluteSubdirectory) ??
                                                  throw new InvalidOperationException(
                                                      "No final directory component in path " + absoluteSubdirectory);
                    return Regex.IsMatch(relativeSubdirectory, @"\A\d+\.\d+\.\d+\.\d+\z");
                });

            return Path.Combine(versionDirectory, "resources", "vivaldi");
        }

        private static bool IsBrowserExecutableDifferent(string processToRun, string renamedProcess)
        {
            var originInfo = new FileInfo(processToRun);
            var destinationInfo = new FileInfo(renamedProcess);
            if (destinationInfo.Exists)
            {
                bool isDateModifiedDifferent = originInfo.LastWriteTimeUtc != destinationInfo.LastWriteTimeUtc;
                bool isSizeDifferent = originInfo.Length != destinationInfo.Length;
                return isDateModifiedDifferent || isSizeDifferent;
            }
            else
            {
                return true;
            }
        }

        private static IEnumerable<string> CustomizeArguments(IEnumerable<string> originalArguments)
        {
            IList<string> customizedArguments = originalArguments.ToList();
            customizedArguments.Insert(0, "--force-renderer-accessibility");
            return customizedArguments;
        }

        private static int CreateProcess(string process, string arguments)
        {
            return Process.Start(process, arguments)?.Id ?? -1;
        }

        private static HttpClient GetClient()
        {
            return _client ?? (_client = new HttpClient());
        }
    }
}