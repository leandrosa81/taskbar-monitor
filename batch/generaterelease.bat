@echo off

echo Generating release files
rem "%programfiles(x86)%\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\sn.exe" -i ..\TaskbarMonitor\leandro.pfx VS_KEY_7B51BC42D066E9FF
"%programfiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild.exe"  /t:Rebuild /p:Configuration=Release /p:VisualStudioVersion=16.0 ..\TaskbarMonitor.sln

copy ..\TaskbarMonitor\bin\Release\TaskbarMonitor.dll ..\build
copy ..\TaskbarMonitorInstaller\bin\Release\TaskbarMonitorInstaller.exe ..\build
rem copy ..\INSTALL.txt ..\build
rem copy .\install.bat ..\build

echo All done.
pause