using System.Collections.Generic;
using VivaldiCustomLauncher;
using Xunit;

namespace Tests;

public class VersionNumberComparerTest {

    private readonly Comparer<string> versionComparer = new VersionNumberComparer();

    [Theory]
    [MemberData(nameof(equalVersions))]
    public void equal(string x, string y) {
        Assert.Equal(0, versionComparer.Compare(x, y));
    }

    public static TheoryData<string, string> equalVersions = new() {
        { "0", "0" },
        { "0", "0.0" },
        { "1", "1" },
        { "0.0", "0.0" },
        { "0.1", "0.1" },
        { "0.01", "0.1" },
        { "0.1", "0.1.0" },
        { "1", "1.0" },
        { "1", "1.0.0" },
        { "1", "1.0.0.0" },
        { "1", "1.0.0.0.0" },
        { "1", "01" },
        { "1", "001" },
        { "1", "0001" },
        { "1", "00001" },
        { "1", "00001.00000" },
        { "1", "00001.00000.00000000000000000000000000000000" },
        { "1.2.3", "1.2.3" },
        { "1.2.3.4", "1.2.3.4" }
    };

    [Theory]
    [MemberData(nameof(lessThanVersions))]
    public void lessThan(string x, string y) {
        Assert.Equal(-1, versionComparer.Compare(x, y));
    }

    [Theory]
    [MemberData(nameof(lessThanVersions))]
    public void greaterThan(string x, string y) {
        Assert.Equal(1, versionComparer.Compare(y, x));
    }

    public static TheoryData<string, string> lessThanVersions = new() {
        { "0", "1" },
        { "0", "1.0" },
        { "0.0", "1" },
        { "0", "0.1" },
        { "0.0", "0.1" },
        { "0.0", "1.0" },
        { "1.2.3", "1.2.4" },
        { "1.2.3", "1.3.0" },
        { "1.2.3", "1.3.1" },
    };

}