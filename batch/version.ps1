#using xml
$version = $args[0]

# create this function in the calling script
function Get-ScriptDirectory { Split-Path $MyInvocation.ScriptName }

$xmlFileName = "..\TaskbarMonitor\Properties\Resources.resx"
# Read the existing file
[xml]$xmlDoc = Get-Content (Join-Path (Get-ScriptDirectory) $xmlFileName)

$xmlDoc.SelectNodes("//root/data[@name=`"Version`"]/value") | % { 
    $_."#text" = $version
    }

$xmlDoc.Save((Join-Path (Get-ScriptDirectory) $xmlFileName))


$xmlFileName = "..\TaskbarMonitorInstaller\Properties\Resources.resx"
# Read the existing file
[xml]$xmlDoc = Get-Content (Join-Path (Get-ScriptDirectory) $xmlFileName)

$xmlDoc.SelectNodes("//root/data[@name=`"Version`"]/value") | % { 
    $_."#text" = $version
    }

$xmlDoc.Save((Join-Path (Get-ScriptDirectory) $xmlFileName))


$xmlFileName = "..\TaskbarMonitorWindows11\Properties\Resources.resx"
# Read the existing file
[xml]$xmlDoc = Get-Content (Join-Path (Get-ScriptDirectory) $xmlFileName)

$xmlDoc.SelectNodes("//root/data[@name=`"Version`"]/value") | % { 
    $_."#text" = $version
    }

$xmlDoc.Save((Join-Path (Get-ScriptDirectory) $xmlFileName))
