using FluentAssertions;
using System.IO;
using VivaldiCustomLauncher;
using Xunit;

namespace Tests;

public class PathTests {

    [Theory]
    [InlineData("https://aldaviva.com", false)]
    [InlineData("https://localhost", false)]
    [InlineData("https://aldaviva.com/a/b.txt", false)]
    [InlineData(@"C:\Programs\Internet\Vivaldi\Application\update_notifier.exe", true)]
    [InlineData("C:/Programs/Internet/Vivaldi/Application/update_notifier.exe", true)]
    [InlineData("update_notifier.exe", true)]
    [InlineData("Application/update_notifier.exe", true)]
    [InlineData(@"\Programs\Internet\Vivaldi\Application\update_notifier.exe", true)]
    [InlineData(@"..\Vivaldi\Application\update_notifier.exe", true)]
    [InlineData(@"C:Application\update_notifier.exe", true)]
    [InlineData(@"\\aegir\C$\", true)]
    [InlineData(@"\\aegir\Ben\Desktop\Untitled2.xml", true)]
    [InlineData(@"\\.\C:\Test\Foo.txt", true)]
    [InlineData(@"\\?\C:\Test\Foo.txt", true)]
    [InlineData(@"\\.\Volume{b75e2c83-0000-0000-0000-602f00000000}\Test\Foo.txt", true)]
    [InlineData(@"\\?\Volume{b75e2c83-0000-0000-0000-602f00000000}\Test\Foo.txt", true)]
    [InlineData(@"\\.\UNC\Server\Share\Test\Foo.txt", true)]
    [InlineData(@"\\?\UNC\Server\Share\Test\Foo.txt", true)]
    public void isFilesystemPathTest(string input, bool expectedIsFilesystemPath) {
        Path.IsFileSystemPath(input).Should().Be(expectedIsFilesystemPath, input);
    }

}