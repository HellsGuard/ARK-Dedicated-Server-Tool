﻿Param
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

$AppVersion = Get-LatestVersion
$AppVersionShort = $AppVersion
$AppVersionShort | Set-Content "$($txtDestFile)"
Write-Host "LatestVersion $($AppVersionShort) ($($AppVersion))"

$versionWithUnderscores = $AppVersion.Replace('.', '_')
$publishSrcDir = "$($rootPath)\$($publishDir)\Application Files\$($filenamePrefix)$($versionWithUnderscores)"
Remove-Item -Path "$($publishSrcDir)\$($srcXmlFilename)" -ErrorAction Ignore

$filenamePrefixStripped = $filenamePrefix.Replace(' ', '')
$zipDestFileName = "$($filenamePrefixStripped)$($AppVersionShort).zip"
$zipDestFile = "$($rootPath)\$($publishDir)\$($zipDestFileName)"
Create-Zip $publishSrcDir $zipDestFile

$batchFileContent = @"
set AWS_DEFAULT_PROFILE=ASMPublish
aws s3 cp "$($zipDestFile)" s3://arkservermanager/beta/latest.zip
aws s3 cp "$($txtDestFile)" s3://arkservermanager/beta/latest.txt
"@

$batchFile = "$env:TEMP\$($filenamePrefixStripped)BetaPublishToFtp.cmd"
$batchFileContent | Out-File -LiteralPath:$batchFile -Force -Encoding ascii

Invoke-Expression -Command:$batchFile

Remove-Item -LiteralPath:$batchFile -Force
