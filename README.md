# taskbar-monitor
DeskBand with monitoring charts (CPU and memory) for Windows

This app shows some cool graphs displaying CPU and memory usage on the taskbar (as a Desk Band).


![Example 1](media/demo.png)

This project was made using the  [CS DeskBand](https://github.com/dsafa/CSDeskBand) library.

## Install instructions
                
To install the taskbar-monitor, first make sure you have <a href="https://dotnet.microsoft.com/download/dotnet-framework/net472" target="_blank" rel="noreferrer">.NET Framework 4.7.2 runtime</a> installed on your computer. If you use Windows 10 1803 April 2018 or later, it is already installed on your computer. 

Then, download the **TaskbarMonitorInstaller.exe** installer and run it.</p>

<p>It needs administrator rights to run, as it installs on your PROGRAM FILES (x86) folder.
The reason for this is that this tool registers itself for all users, so the dll files should be accessible for
    all users on the computer.</p>

## Usage

To enable taskbar-monitor toolbar in your taskbar you have to:

right-click on your taskbar -> select Toolbars -> check "Taskbar Monitor"

## Uninstall
To uninstall it, just use the uninstaller from the "Add or Remove Programs" list.
