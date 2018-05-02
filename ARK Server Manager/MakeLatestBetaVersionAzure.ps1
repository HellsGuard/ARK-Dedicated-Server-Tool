Param
(
    [Parameter()]
    [string]$rootPath = "E:\Development\Projects\GitHub\ARK-Dedicated-Server-Tool\ARK Server Manager",

    [Parameter()]
    [string]$publishDir = "publish",

    [Parameter()]
    [string]$srcXmlFilename = "ARK Server Manager.application",

    [Parameter()]
    [string]$destLatestFilename = "latestbeta.txt",

    [Parameter()]
    [string]$filenamePrefix = "Ark Server Manager_",

    [Parameter()]
    [string]$feedFilename = "VersionFeedBeta.xml",

    [Parameter()]
    [string]$signTool = "C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\SignTool.exe",

    [Parameter()]
    [string]$signFile = "ARK Server Manager.exe",

    [Parameter()]
    [string]$signNFlag = "${env:SIGN_NFLAG}",

    [Parameter()]
    [string]$signTFlag = "http://timestamp.verisign.com/scripts/timstamp.dll",

    [Parameter()]
    [string]$ftpHost = $env:ASM_FTPHOST,

    [Parameter()]
    [string]$ftpPort = "21",

    [Parameter()]
    [string]$ftpUsername = $env:ASM_FTPUSER,

    [Parameter()]
    [string]$ftpPassword = $env:ASM_FTPPASS,

    [Parameter()]
    [string]$ftpPath = "site/wwwroot/downloads/arkservermanager/beta"
)

[string] $AppVersion = ""
[string] $AppVersionShort = ""

function Get-LatestVersion()
{   
    $xmlFile = "$($rootPath)\$($publishDir)\$($srcXmlFilename)"
    $xml = [xml](Get-Content $xmlFile)
    $version = $xml.assembly.assemblyIdentity | Select version
    return $version.version;
}

function Sign-Application ( $sourcedir )
{
	if(Test-Path $signTool)
	{
		if(($signFile -ne "") -and ($signNFlag -ne "") -and ($signTFlag -ne ""))
		{
			Write-Host "Signing $($signFile)"
			& $signTool sign /n "$($signNFlag)" /t $signTFlag "$($sourcedir)\$($signFile)"
		}
	}
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

$txtDestFile = "$($rootPath)\$($publishDir)\$($destLatestFilename)"
$feedFile = "$($rootPath)\$($feedFilename)"
$filenamePrefixStripped = $filenamePrefix.Replace(' ', '')

$AppVersion = Get-LatestVersion
$AppVersionShort = $AppVersion
$AppVersionShort | Set-Content "$($txtDestFile)"
Write-Host "LatestVersion $($AppVersionShort) ($($AppVersion))"

$versionWithUnderscores = $AppVersion.Replace('.', '_')
$publishSrcDir = "$($rootPath)\$($publishDir)\Application Files\$($filenamePrefix)$($versionWithUnderscores)"
Remove-Item -Path "$($publishSrcDir)\$($srcXmlFilename)" -ErrorAction Ignore

Sign-Application $publishSrcDir

$zipDestFileName = "$($filenamePrefixStripped)$($AppVersionShort).zip"
$zipDestFile = "$($rootPath)\$($publishDir)\$($zipDestFileName)"
Create-Zip $publishSrcDir $zipDestFile

$ftpFileContent = @"
open $($ftpHost) $($ftpPort)
$($ftpUsername)
$($ftpPassword)
cd "$($ftpPath)"
put "$($zipDestFile)" "latest.zip"
put "$($txtDestFile)" "latest.txt"
put "$($feedFile)" "VersionFeed.xml"
quit
"@

$ftpFile = "$env:TEMP\$($filenamePrefixStripped)BetaPublishToFtp.ftp"
$ftpFileContent | Out-File -LiteralPath:$ftpFile -Force -Encoding ascii

$batchFileContent = @"
ftp -s:"$($ftpFile)"
"@

$batchFile = "$env:TEMP\$($filenamePrefixStripped)BetaPublishToFtp.cmd"
$batchFileContent | Out-File -LiteralPath:$batchFile -Force -Encoding ascii

Invoke-Expression -Command:$batchFile

Remove-Item -LiteralPath:$ftpFile -Force
Remove-Item -LiteralPath:$batchFile -Force
