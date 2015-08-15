Param
(
    [Parameter(Mandatory = $True)]
    [string]$srcXml,

    [Parameter(Mandatory = $True)]
    [string]$destFile
)

$xmlPath = 'E:\Development\Projects\GitHub\ARK-Dedicated-Server-Tool\ARK Server Manager\publish\Ark Server Manager.application'
$xml = [xml](Get-Content $xmlPath)
$version = $xml.assembly.assemblyIdentity | Select version
$version.version | Set-Content $destFile
