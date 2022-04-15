using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FakeItEasy;
using Tests.Assertions;
using VivaldiCustomLauncher.Tweaks;
using Xunit;
using Xunit.Sdk;

#nullable enable

namespace Tests; 

[SuppressMessage("Microsoft.Design", "UnhandledExceptions:Unhandled exception(s)", Justification = "They're tests")]
public class BackgroundCommonBundleScriptTweakTest {

    private const           string ORIGINAL_BUNDLE_FILENAME = "Data/BundleScript/background-common-bundle.js";
    private static readonly string ORIGINAL_BUNDLE_TEXT     = File.ReadAllText(ORIGINAL_BUNDLE_FILENAME);

    private readonly BackgroundCommonBundleScriptTweak tweak = new();

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
        IList<string>                     fakedCalls = new List<string>();
        BackgroundCommonBundleScriptTweak tweakSpy   = A.Fake<BackgroundCommonBundleScriptTweak>();

        // for this to work, the methods being called by editFile() must be virtual, and the assembly under test must have InternalsVisibleTo DynamicProxyGenAssembly2 (since the methods are internal, not public)
        A.CallTo(tweakSpy).Invokes(call => fakedCalls.Add(call.Method.Name));
        A.CallTo(() => tweakSpy.editFile(A<string>._)).CallsBaseMethod();

        await tweakSpy.editFile(ORIGINAL_BUNDLE_TEXT);

        IEnumerable<MethodInfo> expectedMethods = typeof(BackgroundCommonBundleScriptTweak)
            .GetMethods(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(method => method.Name != nameof(BackgroundCommonBundleScriptTweak.editFile))
            .ToList();

        Assert.NotEmpty(expectedMethods);
        foreach (MethodInfo expectedMethod in expectedMethods) {
            Assert.Contains(expectedMethod.Name, fakedCalls);
        }
    }

    [Fact]
    public void removeExtraSpacingFromTabBarRightSide() {
        string actual = tweak.exposeFolderSubscriptionStatus(ORIGINAL_BUNDLE_TEXT);

        const string EXPECTED =
            @"]={type:n,subscribed:t.subscribed/* Customized by Ben */}}";

        FastAssert.fastAssertSingleReplacementDiff(ORIGINAL_BUNDLE_TEXT, actual, EXPECTED);
    }

}