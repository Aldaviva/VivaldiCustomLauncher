using System;
using System.IO;

namespace Tests.Data;

/// <summary>
/// To run tests against different types of builds, set the <c>BUILD_TYPE</c> environment variable to either <c>release</c> (default) or <c>snapshot</c>.
/// </summary>
public static class DataReader {

    private const string BUILD_TYPE_ENVIRONMENT_VARIABLE_NAME = "BUILD_TYPE";

    /// <summary>
    /// Read a text file from the Tests/Data directory, based on the current <c>BUILD_TYPE</c> environment variable and the given <paramref name="dataFileRelativePath"/> sub-path.
    /// </summary>
    /// <param name="dataFileRelativePath">the path under the <c>Tests\Data\%BUILD_TYPE%\</c> directory, e.g. <c>BundleScript\bundle.js</c></param>
    /// <returns>contents of the given file, decoded with UTF-8</returns>
    public static string readFileTextForCurrentBuildType(string dataFileRelativePath) {
        string buildType = Environment.GetEnvironmentVariable(BUILD_TYPE_ENVIRONMENT_VARIABLE_NAME) ?? "release";
        return File.ReadAllText(Path.Combine("Data", buildType, dataFileRelativePath));
    }

}