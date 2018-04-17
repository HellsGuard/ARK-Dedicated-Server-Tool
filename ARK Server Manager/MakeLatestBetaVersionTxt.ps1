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
    [string]$signTool = "C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\SignTool.exe",

    [Parameter()]
    [string]$signFile = "ARK Server Manager.exe",

    [Parameter()]
    [string]$signNFlag = "${env:SIGN_NFLAG}",

    [Parameter()]
    [string]$signTFlag = "http://timestamp.verisign.com/scripts/timstamp.dll"
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

function Sign-Application ( $sourcePath )
{
	if(Test-Path $signTool)
	{
		if(($signFile -ne "") -and ($signNFlag -ne "") -and ($signTFlag -ne ""))
		{
			Write-Host "Signing $($signFile)"
			& $signTool sign /n "$($signNFlag)" /t $signTFlag "$($sourcePath)\$($signFile)"
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

$AppVersion = Get-LatestVersion
$AppVersionShort = $AppVersion
$AppVersionShort | Set-Content "$($txtDestFile)"
Write-Host "LatestVersion $($AppVersionShort) ($($AppVersion))"

$versionWithUnderscores = $AppVersion.Replace('.', '_')
$publishSrcDir = "$($rootPath)\$($publishDir)\Application Files\$($filenamePrefix)$($versionWithUnderscores)"
Remove-Item -Path "$($publishSrcDir)\$($srcXmlFilename)" -ErrorAction Ignore

Sign-Application $publishSrcDir

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
