@echo OFF
title Install System Watch Band
@echo ON
@setlocal enableextensions
@cd /d "%~dp0"

rem Check permissions
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Administrative permissions confirmed.
) else (
    echo Please run this script with administrator permissions.
	pause
    goto EXIT
)

if defined %PROGRAMFILES(x86)% (
    rem use /unregister to uninstall
    %SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\regasm.exe /nologo /codebase "SystemWatchBand.dll"
) else (
    %SystemRoot%\Microsoft.NET\Framework\v4.0.30319\regasm.exe /nologo /codebase "SystemWatchBand.dll"
)
pause