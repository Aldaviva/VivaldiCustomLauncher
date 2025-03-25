VivaldiCustomLauncher
===

[![CI Build](https://img.shields.io/github/actions/workflow/status/Aldaviva/VivaldiCustomLauncher/ci.yml?branch=master&label=latest%20commit%20build&logo=github)](https://github.com/Aldaviva/VivaldiCustomLauncher/actions/workflows/ci.yml) [![version poll Build](https://img.shields.io/github/actions/workflow/status/Aldaviva/VivaldiCustomLauncher/build.yml?branch=master&label=latest%20Vivaldi%20version%20build&logo=github)](https://github.com/Aldaviva/VivaldiCustomLauncher/actions/workflows/build.yml)

Intercept executions of [Vivaldi](https://vivaldi.com/desktop/) for Windows to add custom arguments and apply tweaks files


<!-- MarkdownTOC autolink="true" bracket="round" autoanchor="false" levels="1,2,3" bullets="1.,-" -->

1. [Usage](#usage)
1. [What does it do?](#what-does-it-do)
1. [How does it work?](#how-does-it-work)
1. [Options](#options)

<!-- /MarkdownTOC -->

## Usage
1. Download `VivaldiCustomLauncher.exe` from the [latest release](https://github.com/Aldaviva/VivaldiCustomLauncher/releases/latest) and save it somewhere, such as `C:\Program Files\Vivaldi\VivaldiCustomLauncher.exe`.
1. Download [`VivaldiCustomLauncher.reg`](https://raw.githubusercontent.com/Aldaviva/VivaldiCustomLauncher/master/VivaldiCustomLauncher.reg).
1. Edit `VivaldiCustomLauncher.reg` to have the correct paths to `VivaldiCustomLauncher.exe`, depending on where you saved it.
1. Merge `VivaldiCustomLauncher.reg` into the registry.
1. Download [SetDefaultBrowser](https://kolbi.cz/blog/2017/11/10/setdefaultbrowser-set-the-default-browser-per-user-on-windows-10-and-server-2016-build-1607/).
1. Run `SetDefaultBrowser.exe hkcu VivaldiCustomLauncher` to make VivaldiCustomLauncher the default browser.
1. Update any shortcuts to `vivaldi.exe` to refer to this program instead, for example, shortcuts in the Start Menu.
1. If you pin Vivaldi to the taskbar, and you see double Vivaldi icons when it's running, then it's because `VivaldiCustomLauncher.exe` is a different executable than `vivaldi.exe`. To fix this, you can set the `VivaldiCustomLauncher.exe` shortcut's AppId to Vivaldi's AppId.
    1. Download [`Win7AppId1.1.exe`](https://code.google.com/archive/p/win7appid/downloads).
    1. Open an elevated Command Prompt, otherwise this program may crash without administrator privileges.
    1. Get Vivaldi's AppId: `Win7AppId1.1.exe "%APPDATA%\Microsoft\Windows\Start Menu\Programs\Vivaldi.lnk"`
    1. Set the VivaldiCustomLauncher shortcut to have the same AppId: `Win7AppId1.1.exe "%APPDATA%\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\Vivaldi.lnk" <Vivaldi's AppId>`.
1. Grant Modify permissions for the Vivaldi installation directory to your Windows user account.
1. Try opening a web page.
1. Tweaks and arguments will be applied.

## What does it do?
- Includes a [custom style sheet](https://github.com/Aldaviva/VivaldiCustomResources/blob/master/style/custom.css) in the browser chrome to clean up the UI and make it more minimal.
- Includes [CSS mods](https://github.com/Aldaviva/VivaldiCustomResources/blob/master/style/mods.css) in the browser chrome to hide the pointless, annoying, ugly Link Copied and Press Esc To Exit Fullscreen toasts. This is a separate stylesheet so that it can also be used in Vivaldi installations that don't use VivaldiCustomLauncher.
- Includes a [custom script](https://github.com/Aldaviva/VivaldiCustomResources/blob/master/scripts/custom.js) to
    - add more keyboard shortcuts to the browser
        |Keyboard shortcut|Action|
        |---|---|
        |`Ctrl`+`Shift`+`C`|Copy current page URL to clipboard|
        |`Ctrl`+`Alt`+`Shift`+`V`|Paste and Go in new tab|
        |`Ctrl`+`E`|Toggle visibility of extension buttons in toolbar|
        |`Alt`+`H`|Hibernate all unpinned background tabs in current window|
        |`Alt`+`Z`|Open history menu (backwards)|
        |`Alt`+`X`|Open history menu (forwards)|
    - send the current tab's URL to my [fork of the KeePass WebAutoType plugin](https://github.com/Aldaviva/WebAutoType) using a localhost AJAX request so KeePass can autotype the correct username and password entry. This is done because the accessibility technique normally used by WebAutoType (MSAA) requires Web Accessibility to be turned on, which frequently makes Vivaldi 3 completely freeze for 20 seconds at a time. Even though the freeze was fixed in Vivaldi 4, the MSAA technique still only works half the time, whereas my plugin works every time.
    - [add a button to the feed preview page](https://github.com/Aldaviva/VivaldiCustomResources/blob/master/scripts/custom-feed.js) so you can subscribe to the page in [Inoreader](https://www.inoreader.com/)
- Tweaks browser scripts to 
    - [make tabs stretch to fill the full width of the tab bar](https://gist.github.com/Aldaviva/39e4472ab7a5ee50473de74df826d928)
    - close the current tab if you use the Back gesture and there are no more pages in the tab's history stack
    - reformat the status shown in the Downloads panel list items to look like `11 seconds, 11.87 MB/s, 17.85/124.71 MB` and `17.85/124 MB - stopped` to put more important information farther to the left so it doesn't get truncated by a narrow panel width
    - navigate to subdomains of the current URL by Ctrl+clicking on the subdomain ([Vivaldi already offers this for parent paths](https://vivaldi.com/blog/vivaldi-introduces-break-mode/), but not subdomains)
    - allow mail to be moved from any folder to any folder, which can be useful for marking messages as spam or not spam on your IMAP server. Destination folders are alphabetized, limited to subscribed folders only, and shown at the top level of the Move To Folder menu instead of in a submenu.
    - classify the "Junk E-mail" folder as a normal folder instead of spam, since I use that as a source of suspected spam on my mail server, not a sink of confirmed spam. This makes the Mark as Spam button work properly, and prevents mail folders from showing the wrong messages.
    - prepend `https://` when you press `Ctrl`+`Enter` in the address bar, in addition to the default behavior of appending `.com`
    - hide incessant, useless status bar messages about checking mail and calendars, which are more annoying than beneficial
    - format data sizes using the widespread conventional base of 1024 instead of 1000 (1 kB = 1024 bytes, 1 MB = 1024 kB, 1 GB = 1024 MB, etc)
- Copies Vivaldi's visual elements manifest XML file so that start menu tiles for this program look like Vivaldi's.
- Automatically reapplies all of the above tweaks if needed when the browser is restarted after installing Vivaldi or an update.

## How does it work?
1. When you open an HTML page, web URL, or a shortcut to Vivaldi, this headless launcher program is started instead, because you updated all the associations and shortcuts for Vivaldi.
1. This program checks to see if the tweaks need to be applied, including after an update. If any of the tweaked files are out-of-date, they are automatically updated.
1. This program launches Vivaldi, passing in your original arguments.

## Options
<dl>
    <dt><code>--vivaldi-application-directory="&lt;dir&gt;"</code></dt>
    <dd>By default, this program finds the Vivaldi installation directory using the registry, but you can customize this (for example, if you have portable or multiple installations) by passing the path to the <code>Applications</code> subdirectory of the Vivaldi installation directory you want to tweak.</dd>
    <dt><code>--do-not-launch-vivaldi</code></dt>
    <dd>By default, this program will install tweaks in the Vivaldi installation directory and then launch Vivaldi. Pass this argument to only install tweaks but not launch Vivaldi.</dd>
    <dt><code>--untweak</code></dt>
    <dd>Uninstall tweaks from the Vivaldi installation directory instead of installing them. Useful if you're trying to determine if a bug was caused by the tweaks or a Vivaldi update.</dd>
    <dt><code>&lt;url&gt;</code></dt>
    <dd>Pass a URL for Vivaldi to launch, or omit it to just start Vivaldi.</dd>
    <dt><code>--help</code>, <code>-h</code>, <code>-?</code></dt>
    <dd>Show usage dialog box.</dd>
    <dt>other arguments</dt>
    <dd>Any arguments not recognized by VivaldiCustomLauncher will be passed through unchanged to Vivaldi, which is useful if you want to run Vivaldi with more logging by passing <code>--enable-logging --v=1</code>.</dd>
</dl>