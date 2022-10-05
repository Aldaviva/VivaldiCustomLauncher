#nullable enable

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: TestFramework("Tests.Assertions.FastAssertCleanup", "Tests")]

namespace Tests.Assertions;

internal static class FastAssert {

    private const int DIFF_EXTRA_CHARACTERS = 64;

    internal static readonly string                TEMP_OUTPUT_DIR         = Environment.ExpandEnvironmentVariables("%temp%\\VivaldiCustomLauncher.Tests\\");
    private static readonly  RandomNumberGenerator RANDOM_NUMBER_GENERATOR = RandomNumberGenerator.Create();

    /// <summary>
    /// Don't print the actual or (optionally) expected strings, they might be several megabytes and freeze the unit test output
    /// </summary>
    /// <param name="assertion">an action that calls an assertion, like the following lambda: <c>() =&gt; Assert.Equal("a", "b")</c></param>
    /// <param name="preserveExpected"><c>true</c> if the expected value from the assertion should be printed, or <c>false</c> if <c>(omitted)</c> should be printed instead. You should set this parameter to <c>false</c> if the expected value is very long.</param>
    /// <param name="writeActualToTempFile">If <c>true</c> and the <c>assertion</c> fails, create a new empty temporary file on disk and write the actual value to it. Its filename will be printed in the exception message so you can open it and inspect it in your favorite text editor. If <c>false</c>, or if the <c>assertion</c> succeeds, don't create any file.</param>
    /// <exception cref="AssertActualExpectedException">if the given Action throws an exception because the assertion fails</exception>
    public static void fastAssert(Action assertion, bool preserveExpected = true, bool writeActualToTempFile = true) {
        try {
            assertion();
        } catch (AssertActualExpectedException e) {
            string? expected = preserveExpected ? e.Expected : "(omitted)";
            string  actual;

            if (writeActualToTempFile) {
                string actualFileName = getTempFileName();
                File.WriteAllText(actualFileName, e.Actual);
                actual = $"(omitted, see {actualFileName})";
            } else {
                actual = "(omitted)";
            }

            AssertActualExpectedException newException = (AssertActualExpectedException) (
                    e.GetType().GetConstructor(new[] { typeof(object), typeof(object) }) ??
                    e.GetType().GetConstructor(new[] { typeof(string), typeof(string) }))!
                .Invoke(new object?[] { expected, actual });

            throw newException;
        }
    }

    /// <summary>
    /// fastAssertSingleReplacement
    /// </summary>
    /// <param name="oldHaystack"></param>
    /// <param name="newHaystack"></param>
    /// <param name="needle"></param>
    /// <param name="writeActualToTempFile"></param>
    /// <exception cref="DoesNotContainException"></exception>
    /// <exception cref="XunitException"></exception>
    public static void fastAssertSingleReplacementDiff(string oldHaystack, string newHaystack, string needle, bool writeActualToTempFile = false) {
        try {
            // Fast sanity check to see if the before-replacement text has the search text in it, and bail out early with an error message with small output
            Assert.DoesNotContain(needle, oldHaystack);
        } catch (DoesNotContainException e) {
            throw new DoesNotContainException(e.Expected, "(too large, omitted)");
        }

        // Fast sanity check to see if the after-replacement text changed at all.
        // Don't use Assert.DoesNotContain() because we don't want to show the entire multi-megabyte string in the test output, since it freezes the ReSharper viewer
        Assert.False(oldHaystack == newHaystack, "Old and new haystacks are the same, so nothing was replaced");

        int newHaystackHeaderIndex  = 0;
        int oldHaystackHeaderIndex  = 0;
        int newHaystackTrailerIndex = newHaystack.Length - 1;
        int oldHaystackTrailerIndex = oldHaystack.Length - 1;

        // Find the first index where the strings differ
        for (char newHaystackChar = default, oldHaystackChar = default;
             newHaystackChar == oldHaystackChar && newHaystackHeaderIndex < newHaystack.Length && oldHaystackHeaderIndex < oldHaystack.Length;
             newHaystackHeaderIndex++, oldHaystackHeaderIndex++) {
            newHaystackChar = newHaystack[newHaystackHeaderIndex];
            oldHaystackChar = oldHaystack[oldHaystackHeaderIndex];
        }

        // Find the last index where the strings differ
        for (char newHaystackChar = default, oldHaystackChar = default;
             newHaystackChar == oldHaystackChar && newHaystackHeaderIndex < newHaystackTrailerIndex && oldHaystackHeaderIndex < oldHaystackTrailerIndex;
             newHaystackTrailerIndex--, oldHaystackTrailerIndex--) {
            newHaystackChar = newHaystack[newHaystackTrailerIndex];
            oldHaystackChar = oldHaystack[oldHaystackTrailerIndex];
        }

        if (!newHaystack.Contains(needle)) {
            string newHaystackDiff = substringWithClipping(newHaystack, newHaystackHeaderIndex - DIFF_EXTRA_CHARACTERS,
                newHaystackTrailerIndex - newHaystackHeaderIndex + 2 * DIFF_EXTRA_CHARACTERS);

            string? actualFileName = null;
            if (writeActualToTempFile) {
                actualFileName = getTempFileName();
                File.WriteAllText(actualFileName, newHaystack);
            }

            throw new XunitException("Expected, but not found:\n" +
                needle +
                "\n\nDiff between original and actual:\n" +
                newHaystackDiff +
                (writeActualToTempFile ? $"\n\nActual output written to {actualFileName}" : ""));
        }
    }

    private static string substringWithClipping(string original, int start, int length) {
        int clippedStart = Math.Min(original.Length, Math.Max(0, start));
        int clippedEnd   = Math.Min(original.Length, Math.Max(0, clippedStart + length));
        return original.Substring(clippedStart, clippedEnd - clippedStart);
    }

    private static string getTempFileName() {
        string candidate;
        Directory.CreateDirectory(TEMP_OUTPUT_DIR);
        do {
            candidate = Path.Combine(TEMP_OUTPUT_DIR, $"tmp{generateRandomString(4)}.txt");
            File.Create(candidate).Dispose();
        } while (!File.Exists(candidate));

        return candidate;
    }

    /// https://stackoverflow.com/a/1344255/979493
    private static string generateRandomString(int length) {
        const string CHARACTERS          = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[]       possibleChars       = CHARACTERS.ToCharArray();
        int          possibleCharsLength = possibleChars.Length;

        StringBuilder result = new(length);

        byte[] randomBuffer = new byte[length * 4];
        RANDOM_NUMBER_GENERATOR.GetBytes(randomBuffer);

        for (int randomByteIndex = 0; randomByteIndex < length; randomByteIndex++) {
            result.Append(possibleChars[BitConverter.ToUInt32(randomBuffer, randomByteIndex * 4) % possibleCharsLength]);
        }

        return result.ToString();
    }

}

// https://stackoverflow.com/a/53143426/979493
internal class FastAssertCleanup: XunitTestFramework {

    public FastAssertCleanup(IMessageSink messageSink): base(messageSink) {
        //clean up before all tests are run
        try {
            Directory.Delete(FastAssert.TEMP_OUTPUT_DIR, true);
        } catch (DirectoryNotFoundException) {
            // already gone
        }
    }

}