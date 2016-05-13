Param
(
    [Parameter()]
    [string]$rootDir = "E:\Development\Projects\GitHub\ARK-Dedicated-Server-Tool\ARK Server Manager",

    [Parameter()]
    [string]$srcXml = "publish\ARK Server Manager.application",

    [Parameter()]
    [string]$destFile = "publish\latest.txt"
)

[string] $AppVersion = ""

function Write-LatestTxt()
{    
    $xml = [xml](Get-Content $srcXml)
    $version = $xml.assembly.assemblyIdentity | Select version
    $version.version | Set-Content $destFile
    return $version.version;
}

function Create-Zip( $zipfilename, $sourcedir )
{
    if(Test-Path $zipfilename)
    {
        Remove-Item -LiteralPath:$zipfilename -Force
    }
   Add-Type -Assembly System.IO.Compression.FileSystem
   Write-Host "Zipping $($sourcedir) into $($zipfilename)"
   $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
   [System.IO.Compression.ZipFile]::CreateFromDirectory($sourcedir,
        $zipfilename, $compressionLevel, $false)
}

$AppVersion = Write-LatestTxt
Write-Host "LatestVersion $($AppVersion)"
$versionWithUnderscores = $AppVersion.Replace('.', '_')
$publishSrcDir = "$($rootDir)\publish\Application Files\Ark Server Manager_$($versionWithUnderscores)"
Remove-Item -Path "$($publishSrcDir)\Ark Server Manager.application"
$publishDestFileName = "ArkServerManager_$($AppVersion).zip"
$publishDestFile = "$($rootDir)\publish\$($publishDestFileName)"
Create-Zip $publishDestFile $publishSrcDir

$batchFileContent = @"
set AWS_DEFAULT_PROFILE=ASMPublisher
aws s3 cp "$($publishDestFile)" s3://arkservermanager/release/
aws s3 cp s3://arkservermanager/release/$($publishDestFileName) s3://arkservermanager/release/latest.zip
aws s3 cp "$($destFile)" s3://arkservermanager/release/
"@

$batchFile = "$env:TEMP\ASMPublishToAWS.cmd"
$batchFileContent | Out-File -LiteralPath:$batchFile -Force -Encoding ascii

Invoke-Expression -Command:$batchFile

Remove-Item -LiteralPath:$batchFile -Force
