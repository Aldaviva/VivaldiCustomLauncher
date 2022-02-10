
# VivaldiCustomLauncher
Intercept executions of Vivaldi to add custom arguments and apply tweaks files

## Usage
1. Download `VivaldiCustomLauncher.exe` from the [latest release](https://github.com/Aldaviva/VivaldiCustomLauncher/releases/latest).
1. Copy `VivaldiCustomLauncher.exe` somewhere.
1. Update your file type and URL scheme associations to use this program instead of `vivaldi.exe`, *e.g.*
    - `HKEY_CLASSES_ROOT\http\shell\open\command`
    - `HKEY_CLASSES_ROOT\https\shell\open\command`
    - `HKEY_CLASSES_ROOT\VivaldiHTM*\shell\open\command`
1. You may want to use [SetDefaultBrowser](https://kolbi.cz/blog/2017/11/10/setdefaultbrowser-set-the-default-browser-per-user-on-windows-10-and-server-2016-build-1607/) to set the scheme and other associations.
1. Update any shortcuts to `vivaldi.exe` to refer to this program instead, for example, shortcuts in the Start Menu.
1. Grant Modify permissions for the Vivaldi installation directory to your Windows user account.
1. Try opening a web page.
1. Tweaks and arguments will be applied.

## What does it do?
- Includes a [custom style sheet](https://github.com/Aldaviva/VivaldiCustomResources/raw/master/style/custom.css) in the browser chrome to clean up the UI and make it more minimal.
- Includes a [custom script](https://github.com/Aldaviva/VivaldiCustomResources/raw/master/scripts/custom.js) to
    - add more keyboard shortcuts to the browser
        |Keyboard shortcut|Action|
        |---|---|
        |`Ctrl`+`Shift`+`C`|Copy current page URL to clipboard|
        |`Ctrl`+`Alt`+`Shift`+`V`|Paste and Go in new tab|
        |`Ctrl`+`E`|Hide extension buttons from toolbar|
        |`Alt`+`H`|Hibernate all unpinned background tabs in the current window|
        |`Alt`+`Z`|Open history menu (backwards)|
        |`Alt`+`X`|Open history menu (forwards)|
    - send the current tab's URL to my [fork of the KeePass WebAutoType plugin](https://github.com/Aldaviva/WebAutoType) using a localhost AJAX request so KeePass can autotype the correct username and password entry. This is done because the accessibility technique normally used by WebAutoType (MSAA) requires Web Accessibility to be turned on, which frequently makes Vivaldi 3 completely freeze for 20 seconds at a time. Even though the freeze was fixed in Vivaldi 4, the MSAA technique still only works half the time, whereas my plugin works every time.
- Tweaks the browser bundle script to 
    - [make tabs stretch to fill the full width of the tab bar](https://gist.github.com/Aldaviva/39e4472ab7a5ee50473de74df826d928)
    - close the current tab if you use the Back gesture and there are no more pages in the history stack
    - reformat the status shown in the Downloads panel list items to look like `11 seconds, 11.87 MB/s, 17.85/124.71 MB` and `17.85/124 MB - stopped` to put more important information farther to the left so it doesn't get truncated by a narrow panel width
    - navigate to subdomains of the current URL by Ctrl+clicking on the subdomain ([Vivaldi already offers this for parent paths](https://vivaldi.com/blog/vivaldi-introduces-break-mode/), but not subdomains)
    - hide unused sections from the Mail panel (All Messages, Custom Folders, Mailing Lists, Filters, Flags, Labels, and Feeds), leaving only All Accounts where all your IMAP folders live
    - allow mail to be moved from any folder to any folder, which can be useful for marking messages as spam or not spam on your IMAP server. Destination folders are alphabetized, limited to subscribed folders only, and shown at the top level of the Move To Folder menu instead of in a submenu.
    - format US phone numbers in the Contacts panel (does not support multi-digit country codes, extensions, or trailing DTMF digits)
- Copies Vivaldi's visual elements manifest XML file so that start menu tiles for this program look like Vivaldi's.
- Automatically reapply all of the above tweaks if needed when the browser is restarted.

## How does it work?
1. When you open an HTML page, web URL, or a shortcut to Vivaldi, this headless launcher program is started instead, because you updated all the associations and shortcuts for Vivaldi.
1. This program checks to see if the tweaks need to be applied, including after an upgrade. If any of the tweaked files are out-of-date, they are automatically updated.
1. This program launches Vivaldi, passing in your original arguments.
