
# VivaldiCustomLauncher
Intercept executions of Vivaldi to add custom arguments and apply tweaks files

This codebase is an absolute mess.

# Usage
1. Download this repo.
1. Import it into Visual Studio Community 2017 or better.
1. Build the solution.
1. Copy `VivaldiCustomLauncher.exe` somewhere.
1. Update your file type and URL scheme associations to use this program instead of `vivaldi.exe`, *e.g.*
	-- `HKEY_CLASSES_ROOT\http\shell\open\command`
    -- `HKEY_CLASSES_ROOT\https\shell\open\command`
    -- `HKEY_CLASSES_ROOT\VivaldiHTM.*\shell\open\command`
1. Update any shortcuts to `vivaldi.exe` to refer to this program instead, for example, shortcuts in the Start Menu.
1. Try opening a web page.
1. Tweaks and arguments will be applied.

# What does it do
- Always runs Vivaldi with the  `--force-renderer-accessibility` argument so [KeePass](https://keepass.info/) can read the URL bar and auto-type passwords. Without this launcher, this is a nonpersistent setting that constantly breaks the password manager.
- Includes a [custom style sheet](https://gist.github.com/Aldaviva/9fbe321331b7f80786a371e0fd4bcfaf#file-style-custom-css) in the browser chrome to clean up the UI and make it more minimal.
- Include a [custom script](https://gist.github.com/Aldaviva/9fbe321331b7f80786a371e0fd4bcfaf#file-scripts-custom-js) to add more keyboard shortcuts to the browser.
- Tweaks the browser bundle script to [make tabs stretch to fill the full width of the tab bar](https://gist.github.com/Aldaviva/39e4472ab7a5ee50473de74df826d928).
- Automatically reapply all of the above tweaks when the browser is restarted or upgraded.

# How does it work
1. When you open an HTML page, web URL, or a shortcut to Vivaldi, this headless launcher program is started instead, because you updated all the associations and shortcuts for Vivaldi.
1. This program checks to see if the tweaks need to be applied, including after an upgrade. If any of the tweaked files are out-of-date, they are automatically updated.
1. This program launches Vivaldi, passing in your original arguments, and adding the default arguments above.
