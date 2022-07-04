@echo off

echo Generating debug files

"%programfiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild.exe"  /t:Rebuild /p:Configuration=Debug /p:VisualStudioVersion=16.0 ..\TaskbarMonitor.sln

copy ..\TaskbarMonitor\bin\Debug\TaskbarMonitor.dll ..\TaskbarMonitorInstaller\Resources
copy ..\TaskbarMonitor\bin\Debug\Newtonsoft.Json.dll ..\TaskbarMonitorInstaller\Resources

"%programfiles(x86)%\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\msbuild.exe"  /t:Rebuild /p:Configuration=Debug /p:VisualStudioVersion=16.0 ..\TaskbarMonitor.sln

copy ..\TaskbarMonitorInstaller\bin\Debug\TaskbarMonitorInstaller.exe ..\build

echo All done.
pause