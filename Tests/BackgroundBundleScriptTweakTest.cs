#nullable enable

using FakeItEasy;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Tests.Assertions;
using Tests.Data;
using VivaldiCustomLauncher.Tweaks;
using Xunit;
using Xunit.Sdk;

namespace Tests;

public class BackgroundBundleScriptTweakTest {

    private static readonly string ORIGINAL_BUNDLE_TEXT = DataReader.readFileTextForCurrentBuildType("BundleScript/background-bundle.js");

    private readonly BackgroundBundleScriptTweak tweak = new();

    [Fact]
    public void originalBundleWasNotCustomized() {
        try {
            Assert.DoesNotContain("Customized by Ben", ORIGINAL_BUNDLE_TEXT);
        } catch (DoesNotContainException e) {
            throw new DoesNotContainException(e.Expected, "(omitted)");
        }
    }

    [Fact]
    public async Task allTweaksAreCalled() {
        IList<string>               fakedCalls = new List<string>();
        BackgroundBundleScriptTweak tweakSpy   = A.Fake<BackgroundBundleScriptTweak>();

        // for this to work, the methods being called by editFile() must be virtual, and the assembly under test must have InternalsVisibleTo DynamicProxyGenAssembly2 (since the methods are internal, not public)
        A.CallTo(tweakSpy).Invokes(call => fakedCalls.Add(call.Method.Name));
        A.CallTo(() => tweakSpy.editFile(A<string>._)).CallsBaseMethod();

        await tweakSpy.editFile(ORIGINAL_BUNDLE_TEXT);

        IEnumerable<MethodInfo> expectedMethods = typeof(BackgroundBundleScriptTweak)
            .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(method => method.Name != nameof(BackgroundBundleScriptTweak.editFile))
            .Where(method => method.ReturnType == typeof(string))
            .Where(method => method.GetCustomAttribute<ObsoleteAttribute>() == null)
            .ToList();

        Assert.NotEmpty(expectedMethods);
        foreach (MethodInfo expectedMethod in expectedMethods) {
            Assert.Contains(expectedMethod.Name, fakedCalls);
        }
    }

    [Fact]
    public void classifyJunkEmailAsNormalFolder() {
        string actual = tweak.classifyJunkEmailAsNormalFolder(ORIGINAL_BUNDLE_TEXT);

        const string EXPECTED = """function xt(e){if(e.path === "Junk E-mail"){ return false; }/* Customized by Ben */if(e.flags)for(let """;

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
    }

    [Fact]
    public async Task deleteServiceWorkerScriptCacheOnChange() {
        const string LOCAL_APP_DATA       = "LOCALAPPDATA";
        string       originalLocalAppData = Environment.GetEnvironmentVariable(LOCAL_APP_DATA)!;
        string       fakeBundleFile       = Path.GetTempFileName();
        string       fakeLocalAppData     = Path.Combine(Path.GetTempPath(), "VivaldiCustomLauncherTest", "deleteServiceWorkerScriptCacheOnChange");
        string       fakeScriptCache      = Path.Combine(fakeLocalAppData, @"Vivaldi\User Data\Default\Storage\ext\mpognobbkildjkofajifpdfhcoklimli\def\Service Worker\ScriptCache\");
        string       backedUpCacheFile    = Path.Combine(fakeScriptCache, "..", "..", "Service Worker-old", "ScriptCache", "fakeCacheFile");
        try {
            Directory.CreateDirectory(fakeScriptCache);
            string fakeCacheFile = Path.Combine(fakeScriptCache, "fakeCacheFile");
            File.WriteAllText(fakeCacheFile, "cached service worker file");
            Environment.SetEnvironmentVariable(LOCAL_APP_DATA, fakeLocalAppData);

            BackgroundBundleScriptTweak fakeTweak = A.Fake<BackgroundBundleScriptTweak>();
            A.CallTo(() => fakeTweak.readAndEditFile(A<BaseTweakParams>._)).CallsBaseMethod();
            A.CallTo(() => fakeTweak.editFile(A<string>._)).Returns("fake non-null new file contents");

            await fakeTweak.readAndEditFile(new BaseTweakParams(fakeBundleFile));

            Directory.Exists(fakeScriptCache).Should().BeFalse();
            File.ReadAllText(backedUpCacheFile).Should().Be("cached service worker file");
        } finally {
            Directory.Delete(Path.Combine(Path.GetTempPath(), "VivaldiCustomLauncherTest"), true);
            Environment.SetEnvironmentVariable(LOCAL_APP_DATA, originalLocalAppData);
        }
    }

}