Param
(
    [Parameter()]
    [string]$rootPath = "E:\Development\Projects\GitHub\ARK-Dedicated-Server-Tool\Plugin.Discord",

    [Parameter()]
    [string]$binDir = "bin\Release",

    [Parameter()]
    [string]$publishDir = "Publish",

    [Parameter()]
    [string]$srcFilename = "ArkServerManager.Plugin.Discord.dll",

    [Parameter()]
    [string]$destLatestFilename = "latestbeta.txt",

    [Parameter()]
    [string]$filenamePrefix = "ArkServerManager.Plugin.Discord",

    [Parameter()]
    [string]$ftpHost = $env:ASM_FTPHOST,

    [Parameter()]
    [string]$ftpPort = "21",

    [Parameter()]
    [string]$ftpUsername = $env:ASM_FTPUSER,

    [Parameter()]
    [string]$ftpPassword = $env:ASM_FTPPASS,

    [Parameter()]
    [string]$ftpPath = "site/wwwroot/downloads/discordplugin/beta"
)

[string] $AppVersion = ""
[string] $AppVersionShort = ""

function Get-LatestVersion( $srcFile )
{   
	$assembly = [Reflection.Assembly]::Loadfile($srcFile)
	$assemblyName = $assembly.GetName()
	return $assemblyName.version;
}

function Create-Zip( $sourcePath , $zipFile )
{
    if(Test-Path $zipFile)
    {
        Remove-Item -LiteralPath:$zipFile -Force
    }
	Add-Type -Assembly System.IO.Compression.FileSystem
	Write-Host "Zipping $($sourcePath) into $($zipFile)"
	$compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
	[System.IO.Compression.ZipFile]::CreateFromDirectory($sourcePath, $zipFile, $compressionLevel, $false)
}

$srcFile = "$($rootPath)\$($binDir)\$($srcFilename)"

$AppVersion = Get-LatestVersion $srcFile
$AppVersionShort = $AppVersion
Write-Host "LatestVersion $($AppVersionShort) ($($AppVersion))"

# test if the publish directory exists
$versionWithUnderscores = $AppVersion.Replace('.', '_')
$publishPath = "$($rootPath)\$($publishDir)\$($filenamePrefix)_$($versionWithUnderscores)"
if(!(Test-Path -Path ($publishPath)))
{
  Write-Host "Creating folder $($publishPath)"

  # create the destination directory
  New-Item -ItemType directory -Path "$($publishPath)"
}

# copy the source file
Copy-Item -Path $($srcFile) -Destination $($publishPath) -Force
Write-Host "Copied $($srcFile) to $($publishPath)"

# write latest version file
$txtDestFileName = "$($destLatestFilename)"
$txtDestFile = "$($rootPath)\$($publishDir)\$($txtDestFileName)"
$AppVersionShort | Set-Content "$($txtDestFile)"

# create the zip file
$zipDestFileName = "$($filenamePrefix)_$($AppVersionShort).zip"
$zipDestFile = "$($rootPath)\$($publishDir)\$($zipDestFileName)"
Create-Zip $publishPath $zipDestFile

$ftpFileContent = @"
open $($ftpHost) $($ftpPort)
$($ftpUsername)
$($ftpPassword)
cd "$($ftpPath)"
put "$($zipDestFile)" "$($filenamePrefix).zip"
put "$($txtDestFile)" "$($filenamePrefix).txt"
quit
"@

$ftpFile = "$env:TEMP\$($filenamePrefix)_BetaPublishToFtp.ftp"
$ftpFileContent | Out-File -LiteralPath:$ftpFile -Force -Encoding ascii

$batchFileContent = @"
ftp -s:"$($ftpFile)"
"@

$batchFile = "$env:TEMP\$($filenamePrefix)_BetaPublishToFtp.cmd"
$batchFileContent | Out-File -LiteralPath:$batchFile -Force -Encoding ascii

Invoke-Expression -Command:$batchFile

Remove-Item -LiteralPath:$ftpFile -Force
Remove-Item -LiteralPath:$batchFile -Force
