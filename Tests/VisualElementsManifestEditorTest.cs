using System;
using System.IO;
using VivaldiCustomLauncher;
using Xunit;
using Xunit.Abstractions;

namespace Tests {

    public class VisualElementsManifestEditorTest {

        private readonly VisualElementsManifestEditor editor = new VisualElementsManifestEditor();
        private readonly ITestOutputHelper testOutputHelper;

        private readonly ApplicationManifest vivaldiApplication = new ApplicationManifest {
            visualElements = new VisualElements {
                showNameOnSquare150X150Logo = "on",
                square150X150Logo = "3.1.1929.40\\VisualElements\\Logo.png",
                square70X70Logo = "3.1.1929.40\\VisualElements\\SmallLogo.png",
                square44X44Logo = "3.1.1929.40\\VisualElements\\SmallLogo.png",
                foregroundText = "light",
                backgroundColor = "#EF3939"
            }
        };

        public VisualElementsManifestEditorTest(ITestOutputHelper testOutputHelper) {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void load() {
            const string INPUT = @"<Application xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>
  <VisualElements
      ShowNameOnSquare150x150Logo='on'
      Square150x150Logo='3.1.1929.40\VisualElements\Logo.png'
      Square70x70Logo='3.1.1929.40\VisualElements\SmallLogo.png'
      Square44x44Logo='3.1.1929.40\VisualElements\SmallLogo.png'
      ForegroundText='light'
      BackgroundColor='#EF3939'/>
</Application>
";

            string tempFileName = Path.GetTempFileName();
            File.WriteAllText(tempFileName, INPUT);

            ApplicationManifest actual = editor.load(tempFileName);

            Assert.Equal("on", actual.visualElements.showNameOnSquare150X150Logo);
            Assert.Equal("3.1.1929.40\\VisualElements\\Logo.png", actual.visualElements.square150X150Logo);
            Assert.Equal("3.1.1929.40\\VisualElements\\SmallLogo.png", actual.visualElements.square70X70Logo);
            Assert.Equal("3.1.1929.40\\VisualElements\\SmallLogo.png", actual.visualElements.square44X44Logo);
            Assert.Equal("light", actual.visualElements.foregroundText);
            Assert.Equal("#EF3939", actual.visualElements.backgroundColor);

            File.Delete(tempFileName);
        }

        [Fact]
        public void save() {
            // This is slightly differentn from INPUT in load() above, because the C# XML serializer isn't customizable enough to serialize it exactly like the original file from Vivaldi:
            // - attributes on their own line are indented one extra level (2 spaces), not 2 levels (4 spaces)
            // - attribute values are surrounded by double quotation marks ("), not single quotation marks (')
            // - self-closing elements have a space before the end of the tag (" />) instead of no space ("/>)
            // - the file does not have a trailing newline
            const string EXPECTED = @"<Application xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"">
  <VisualElements
    ShowNameOnSquare150x150Logo=""on""
    Square150x150Logo=""3.1.1929.40\VisualElements\Logo.png""
    Square70x70Logo=""3.1.1929.40\VisualElements\SmallLogo.png""
    Square44x44Logo=""3.1.1929.40\VisualElements\SmallLogo.png""
    ForegroundText=""light""
    BackgroundColor=""#EF3939"" />
</Application>";

            string tempFileName = Path.GetTempFileName();
            editor.save(vivaldiApplication, tempFileName);

            string actual = File.ReadAllText(tempFileName);
            testOutputHelper.WriteLine(actual);
            Assert.Equal(EXPECTED, actual);
        }
        
        [Fact]
        public void relativizeUris() {
            editor.relativizeUris(vivaldiApplication, "Application");

            VisualElements visualElements = vivaldiApplication.visualElements;

            Assert.Equal("Application\\3.1.1929.40\\VisualElements\\Logo.png", visualElements.square150X150Logo);
            Assert.Equal("Application\\3.1.1929.40\\VisualElements\\SmallLogo.png", visualElements.square70X70Logo);
            Assert.Equal("Application\\3.1.1929.40\\VisualElements\\SmallLogo.png", visualElements.square44X44Logo);

            // these are unchanged
            Assert.Equal("on", visualElements.showNameOnSquare150X150Logo);
            Assert.Equal("light", visualElements.foregroundText);
            Assert.Equal("#EF3939", visualElements.backgroundColor);
        }

    }

}