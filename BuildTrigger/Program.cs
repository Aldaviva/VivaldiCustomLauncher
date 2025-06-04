using McMaster.Extensions.CommandLineUtils;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.XPath;
using Unfucked;
using Unfucked.HTTP;
using Unfucked.HTTP.Exceptions;

namespace VivaldiCustomLauncher.BuildTrigger;

public class Program(string? gitHubAccessToken, bool isDryRun) {

    private static readonly XmlNamespaceManager NAMESPACES = new(new NameTable());
    private static readonly Uri WORKFLOW_BASE_URI = new("https://api.github.com/repos/Aldaviva/VivaldiCustomLauncher/actions/");
    private static readonly UrlBuilder TESTED_VERSION = UrlBuilder.FromTemplate("https://raw.githubusercontent.com/Aldaviva/VivaldiCustomLauncher/master/Tests/Data/vivaldi-{buildType}-version.txt");
    private static readonly UrlBuilder SPARKLE = UrlBuilder.FromTemplate("https://update.vivaldi.com/update/1.0/{channel}/appcast.x64.xml");

    static Program() {
        NAMESPACES.AddNamespace("sparkle", "http://www.andymatuschak.org/xml-namespaces/sparkle");
    }

    private readonly HttpClient httpClient = new UnfuckedHttpClient {
            DefaultRequestHeaders = { UserAgent = { new ProductInfoHeaderValue("(+mailto:ben@aldaviva.com)") } }
        }
        .Register(new GitHubApiFilter(gitHubAccessToken!));

    public static async Task<int> Main(string[] args) {
        CommandLineApplication argumentParser = new();
        CommandOption<string> gitHubAccessTokenOption =
            argumentParser.Option<string>("--github-access-token", "Token with repo scope access to Aldaviva/VivaldiCustomLauncher", CommandOptionType.SingleValue);
        CommandOption<bool> dryRunOption = argumentParser.Option<bool>("-n|--dry-run", "Don't actually start any builds", CommandOptionType.NoValue);
        argumentParser.Parse(args);

        if (gitHubAccessTokenOption.Value() is { } gitHubAccessToken) {
            await new Program(gitHubAccessToken, dryRunOption.ParsedValue).buildIfOutdated();
            return 0;
        } else {
            Console.WriteLine($"Usage: {Process.GetCurrentProcess().ProcessName} --github-access-token XXXXXXXXX [--dry-run]");
            return 1;
        }
    }

    private async Task buildIfOutdated() {
        foreach (BuildType buildType in Enum.GetValues<BuildType>()) {
            bool wasBuildTriggered = await buildIfOutdated(buildType);
            if (wasBuildTriggered) {
                break;
            }
        }
    }

    /// <returns><c>true</c> if a build was triggered, or <c>false</c> if it was not</returns>
    private async Task<bool> buildIfOutdated(BuildType buildType) {
        // don't immediately await method calls because these two requests should run in parallel
        string       latestVivaldiVersion, testedVivaldiVersion;
        Task<string> latestVivaldiVersionTask = getLatestVivaldiVersionSparkle(buildType);
        Task<string> testedVivaldiVersionTask = getTestedVivaldiVersion(buildType);

        try {
            latestVivaldiVersion = await latestVivaldiVersionTask;
        } catch (HttpException e) {
            Console.WriteLine($"Failed to check latest Vivaldi version: {e.Message}");
            return false;
        }

        try {
            testedVivaldiVersion = await testedVivaldiVersionTask;
        } catch (HttpException e) {
            Console.WriteLine($"Failed to check tested Vivaldi version: {e.Message}");
            return false;
        }

        if (latestVivaldiVersion == testedVivaldiVersion) {
            Console.WriteLine($"{buildType.ToString().ToLower().ToUpperFirstLetter()} is up-to-date, not triggering {buildType.ToString().ToLower()} build.");
            return false;
        } else if (await isBuildRunning()) {
            Console.WriteLine($"Project is already building now, not triggering {buildType.ToString().ToLower()} build.");
            return false;
        } else {
            await triggerBuild(buildType);
            return true;
        }
    }

    private async Task<string> getTestedVivaldiVersion(BuildType buildType) {
        string version = (await httpClient.Target(TESTED_VERSION)
                .ResolveTemplate("buildType", buildType.ToString().ToLower())
                .Get<string>())
            .Trim();

        Console.WriteLine($"Tested Vivaldi {buildType.ToString().ToLower()} version: {version}");
        return version;
    }

    // about 550ms without an existing connection, about 180ms with keep-alive
    private async Task<string> getLatestVivaldiVersionSparkle(BuildType buildType) {
        XPathNavigator xpath = await httpClient.Target(SPARKLE)
            .ResolveTemplate("channel", buildType == BuildType.RELEASE ? "public" : "win")
            .Get<XPathNavigator>();

        string version = xpath.SelectSingleNode("/rss/channel/item/enclosure/@sparkle:version", NAMESPACES)!.Value;
        Console.WriteLine($"Latest Vivaldi {buildType.ToString().ToLower()} version: {version}");
        return version;
    }

    private async Task<bool> isBuildRunning() {
        using JsonDocument responseJson = await httpClient.Target(WORKFLOW_BASE_URI)
            .Path("runs")
            .QueryParam("per_page", 10)
            .Get<JsonDocument>();
        JsonElement runs = responseJson.RootElement.GetProperty("workflow_runs");

        return runs.EnumerateArray().Any(run => {
            string latestBuildStatusRaw = run.GetProperty("status").GetString()!;
            Enum.TryParse(latestBuildStatusRaw, true, out WorkflowStatus latestBuildStatus);
            return latestBuildStatus is WorkflowStatus.IN_PROGRESS or WorkflowStatus.QUEUED or WorkflowStatus.REQUESTED or WorkflowStatus.WAITING;
        });
    }

    private async Task triggerBuild(BuildType buildType) {
        JsonObject requestBody = new() {
            {
                "ref", JsonValue.Create("master")
            }, {
                "inputs", new JsonObject {
                    { "buildType", JsonValue.Create(buildType.ToString().ToLower()) }
                }
            }
        };

        if (!isDryRun) {
            (await httpClient.Target(WORKFLOW_BASE_URI)
                .Path("workflows/build.yml/dispatches")
                .Post(JsonContent.Create(requestBody))).Dispose();
        }

        Console.WriteLine("Build triggered.");
    }

}