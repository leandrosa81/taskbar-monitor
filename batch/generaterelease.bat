@echo off

echo Generating release files
copy ..\SystemWatchBand\bin\Release\SystemWatchBand.dll ..\build
copy ..\INSTALL.txt ..\build
copy .\install.bat ..\build

echo All done.
