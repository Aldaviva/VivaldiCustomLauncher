#nullable enable

using System;

namespace VivaldiCustomLauncher;

public class VersionManifest {

    public Version launcherVersion { get; set; } = null!;
    public string resourcesCommitHash { get; set; } = null!;
    public Version? browserVersion { get; set; }

    /// <summary>
    /// Parameterless constructor for deserializing
    /// </summary>
    public VersionManifest() { }

    public VersionManifest(Version launcherVersion, string resourcesCommitHash, Version browserVersion): this() {
        this.launcherVersion     = launcherVersion;
        this.resourcesCommitHash = resourcesCommitHash;
        this.browserVersion      = browserVersion;
    }

}