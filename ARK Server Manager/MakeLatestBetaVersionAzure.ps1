Param
(
    [Parameter()]
    [string]$rootPath = "E:\Development\Projects\GitHub\ARK-Dedicated-Server-Tool\ARK Server Manager",

    [Parameter()]
    [string]$filenamePrefix = "ArkServerManager_",

    [Parameter()]
    [string]$feedFilename = "VersionFeedBeta.xml",

    [Parameter()]
    [string]$ftpHost = $env:SM_FTPHOST,

    [Parameter()]
    [string]$ftpPort = "21",

    [Parameter()]
    [string]$ftpUsername = $env:SM_FTPUSER,

    [Parameter()]
    [string]$ftpPassword = $env:SM_FTPPASS,

    [Parameter()]
    [string]$ftpPath = "site/wwwroot/downloads/arkservermanager/beta"
)

$feedFile = "$($rootPath)\$($feedFilename)"

$ftpFileContent = @"
open $($ftpHost) $($ftpPort)
$($ftpUsername)
$($ftpPassword)
cd "$($ftpPath)"
put "$($feedFile)" "VersionFeed.xml"
quit
"@

$ftpFile = "$env:TEMP\$($filenamePrefix)BetaPublishToFtp.ftp"
$ftpFileContent | Out-File -LiteralPath:$ftpFile -Force -Encoding ascii

$batchFileContent = @"
ftp -s:"$($ftpFile)"
"@

$batchFile = "$env:TEMP\$($filenamePrefix)BetaPublishToFtp.cmd"
$batchFileContent | Out-File -LiteralPath:$batchFile -Force -Encoding ascii

Invoke-Expression -Command:$batchFile

Remove-Item -LiteralPath:$ftpFile -Force
Remove-Item -LiteralPath:$batchFile -Force
