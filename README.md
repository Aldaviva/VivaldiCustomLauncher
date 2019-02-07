# VivaldiCustomLauncher
Intercept executions of Vivaldi to add custom arguments and apply tweaks files

This codebase is an absolute mess.

# Usage
1. Download this repo.
1. Import it into Visual Studio Community 2017 or better.
1. Build the solution.
1. Copy `VivaldiCustomLauncher.exe` somewhere.
1. Run `VivaldiCustomLauncher.exe`.
1. Enable hijacking.
1. Try running Vivaldi.
1. Tweaks and arguments will be applied.

# What does it do
- Always run Vivaldi with the  `--force-renderer-accessibility` argument so KeePass can read the URL bar and auto-type passwords. Otherwise, this is a nonpersistent setting that constantly breaks the password manager.
- Include a [custom style sheet](https://gist.github.com/Aldaviva/9fbe321331b7f80786a371e0fd4bcfaf#file-style-custom-css) in the browser chrome to clean up the UI and make it more minimalistic.
- Tweak the browser bundle script to [make tabs stretch to fill the full width of the tab bar](https://gist.github.com/Aldaviva/39e4472ab7a5ee50473de74df826d928).
- Automatically reapply all of the above tweaks when the browser is restarted or upgraded.

# How does it work
1. The hijack button sets an Image File Execution Option on Vivaldi. This way, when you try to start Vivaldi, Windows runs this program instead.
1. This program checks to see if the tweaks need to be reapplied after an upgrade. If any of the tweaked files are out-of-date, they are automatically updated.
1. This program makes a copy of the Vivaldi executable, which is an implementation requirement of using Image File Execution Options.
1. This program launches Vivaldi, passing in your original arguments, and adding the default arguments above.
