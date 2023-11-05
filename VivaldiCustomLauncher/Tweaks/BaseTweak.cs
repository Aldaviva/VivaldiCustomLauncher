using System.Text;

namespace VivaldiCustomLauncher.Tweaks;

public class BaseTweak {

    protected static readonly UTF8Encoding UTF8_READING = new(true, true);
    protected static readonly UTF8Encoding UTF8_WRITING = new(false, true);

}