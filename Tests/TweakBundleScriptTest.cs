using System.IO;
using System.Threading.Tasks;
using VivaldiCustomLauncher.Tweaks;
using Xunit;

#nullable enable

namespace Tests {

    public class TweakBundleScriptTest {

        private const string ORIGINAL_BUNDLE_FILENAME = "Data/BundleScript/original-bundle.js";
        private readonly BundleScriptTweak tweak = new BundleScriptTweak();

        [Fact]
        public void removeExtraSpacingFromTabBarRightSide() {
            string input = File.ReadAllText(ORIGINAL_BUNDLE_FILENAME);

            string actual = tweak.removeExtraSpacingFromTabBarRightSide(input);

            const string EXPECTED = @"F(this,""getStyles"",e=>this.createFlexBoxLayout(this.props.tabs,this.props.direction,this.props.maxWidth+62/* Customized by Ben */,this.props.maxHeight,{";

            Assert.DoesNotContain(EXPECTED, input);
            Assert.Contains(EXPECTED, actual);
        }

        [Fact]
        public void increaseMaximumTabWidth() {
            string input = File.ReadAllText(ORIGINAL_BUNDLE_FILENAME);

            string actual = tweak.increaseMaximumTabWidth(input);

            const string EXPECTED = @"return t?(r.maxWidth=4000/* Customized by Ben */,r.maxHeight=";

            Assert.DoesNotContain(EXPECTED, input);
            Assert.Contains(EXPECTED, actual);
        }

        [Fact]
        public async Task closeTabOnBackGestureIfNoTabHistory() {
            string input = File.ReadAllText(ORIGINAL_BUNDLE_FILENAME);

            string actual = await tweak.closeTabOnBackGestureIfNoTabHistory(input);
            
            const string EXPECTED = @"{name:""COMMAND_PAGE_BACK"",action:()=>{const c=g.a.getActivePage(),e=c&&a(97).a.getNavigationInfo(c.id);e&&e.canGoBack?Ce.a.back():p.a.close()}/* Customized by Ben */,category:be.a.CATEGORY_COMMAND_WEBPAGE_NAVIGATION,...Object(Ie.a)(""History Back"")},";
            
            Assert.DoesNotContain(EXPECTED, input);
            Assert.Contains(EXPECTED, actual);
        }

    }

}