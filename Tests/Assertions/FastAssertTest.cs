using System.Diagnostics.CodeAnalysis;
using Xunit;
using Xunit.Sdk;

namespace Tests.Assertions; 

[SuppressMessage("Microsoft.Design", "UnhandledExceptions:Unhandled exception(s)", Justification = "They're tests")]
public class FastAssertTest {

    [Fact]
    public void fastAssertSingleReplacementDiffEnsuresOldHaystackDoesNotContainNeedle() {
        const string OLD_HAYSTACK = "abc";
        const string NEW_HAYSTACK = "abcd";
        const string NEEDLE       = "b";
        Assert.Throws<DoesNotContainException>(() => FastAssert.fastAssertSingleReplacementDiff(OLD_HAYSTACK, NEW_HAYSTACK, NEEDLE));
    }

    [Fact]
    public void fastAssertSingleReplacementDiffEnsuresOldAndNewHaystacksAreDifferent() {
        const string OLD_HAYSTACK = "abc";
        const string NEW_HAYSTACK = "abc";
        const string NEEDLE       = "d";
        Assert.Throws<FalseException>(() => FastAssert.fastAssertSingleReplacementDiff(OLD_HAYSTACK, NEW_HAYSTACK, NEEDLE));
    }

    [Fact]
    public void fastAssertSingleReplacementDiffDoesNotThrowWhenNeedleIsFound() {
        const string OLD_HAYSTACK = "abc";
        const string NEW_HAYSTACK = "adc";
        const string NEEDLE       = "d";
        FastAssert.fastAssertSingleReplacementDiff(OLD_HAYSTACK, NEW_HAYSTACK, NEEDLE);
    }

    [Fact]
    public void fastAssertSingleReplacementDiffShowsDiffWhenNeedleIsNotFound() {
        const string OLD_HAYSTACK = "abc";
        const string NEW_HAYSTACK = "adc";
        const string NEEDLE       = "e";
        try {
            FastAssert.fastAssertSingleReplacementDiff(OLD_HAYSTACK, NEW_HAYSTACK, NEEDLE);
            Assert.False(true, "should have thrown a ContainsException above");
        } catch (XunitException e) {
            Assert.Equal("Expected, but not found:\ne\n\nDiff between original and actual:\nadc", e.Message);
        }
    }

}