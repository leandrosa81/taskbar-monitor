@echo off

echo Generating release files
rem "%programfiles(x86)%\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\sn.exe" -i ..\SystemWatchBand\leandro.pfx VS_KEY_7B51BC42D066E9FF
rem "%programfiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild.exe"  /t:Rebuild /p:Configuration=Release /p:VisualStudioVersion=16.0 ..\SystemWatchBand.sln

copy ..\SystemWatchBand\bin\Release\SystemWatchBand.dll ..\build
copy ..\INSTALL.txt ..\build
copy .\install.bat ..\build

echo All done.
pause