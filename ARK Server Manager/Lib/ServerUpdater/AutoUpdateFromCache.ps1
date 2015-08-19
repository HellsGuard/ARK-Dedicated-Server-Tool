Param
(
    [Parameter(Mandatory = $False)]
    [string]$ServerCache,

    [Parameter(Mandatory = $False)]
    [string]$InstallDirectory,

    [Parameter(Mandatory = $False)]
    [string]$rconIP,

    [Parameter(Mandatory = $False)]
    [string]$rconPort,

    [Parameter(Mandatory = $False)]
    [string]$rconPass,

    [Parameter(Mandatory = $False)]
    $graceMinutes,

    [Parameter(Mandatory = $False)]
    [string]$mcrconPath = "mcrcon.exe",

    [Parameter(Mandatory = $False)]
    [string]$LastUpdatedTimeFileName = "LastUpdated.txt",

    [Parameter(Mandatory = $False)]
    [string]$LastCheckedTimeFileName = "LastChecked.txt",

    [Parameter(Mandatory = $False)]
    [string]$CacheUpdateInProgressFileName = "CacheUpdateInProgress.txt"
)

& whoami

$updateInProgressFile = "$($InstallDirectory)\UpdateInProgress.txt"

function Send-RCON($message)
{
    Write-Host "RCON: $($message)"
    & $ServerCache\$mcrconPath -c -H $rconIP -P $rconPort -p $rconPass $message
}

function Write-Server($minutes)
{    
    Send-RCON "broadcast [SERVER] Update available.  Rebooting in $($minutes) minutes..."
}

     
if(Test-Path $updateInProgressFile)
{
    Write-Host "Update already in progress.  If this is in error, delete $($updateInProgressFile) and re-run."
}
else
{
    try
    {
        Get-Date | Out-File $updateInProgressFile

        #
        # Wait for the cache to be available.  NOTE: This doesn't prevent a download from starting while we are copying...
        #
        while(Test-Path "$($ServerCache)\$($CacheUpdateInProgressFileName)")
        {
            Write-Host "Waiting for cache..."
            Start-Sleep -Seconds 5
        }

        $localLastUpdatedFile = "$($InstallDirectory)\$($LastUpdatedTimeFileName)"
        if(Test-Path $localLastUpdatedFile)
        {
            [string]$timeString = Get-Content $localLastUpdatedFile
            $localTime = [System.DateTime]::Parse($timeString, [System.Globalization.CultureInfo]::CurrentCulture, [System.Globalization.DateTimeStyles]::RoundtripKind);
        }
        else
        {
            $localTime = [System.DateTime]::MinValue
        }

        $cacheLastUpdatedFile = "$($ServerCache)\$($LastUpdatedTimeFileName)"
        [string]$timeString = Get-Content $cacheLastUpdatedFile
        $cacheTime = [System.DateTime]::Parse($timeString, [System.Globalization.CultureInfo]::CurrentCulture, [System.Globalization.DateTimeStyles]::RoundtripKind);

        Write-Host "Cache Time: $($cacheTime)"
        Write-Host "Local Time: $($localTime)"

        if($cacheTime -gt $localTime)
        {
            # Find the process
            $process = Get-WmiObject Win32_Process -Filter "name = 'ShooterGameServer.exe'" | Where {$_.CommandLine -match "$($InstallDirectory -replace "\\", "\\")" }
            if($process -ne $null)
            {
                $minutesLeft = $graceMinutes
                while($minutesLeft -gt 0)
                {
                    Write-Server($minutesLeft);
                    if($minutesLeft > 10)
                    {
                        Start-Sleep -Seconds 300
                        $minutesLeft = $minutesLeft - 5
                    }
                    else
                    {
                        Start-Sleep -Seconds 60
                        $minutesLeft = $minutesLeft - 1
                    }
                }

                Send-RCON "saveworld"
                Start-Sleep -Seconds 15
                Send-RCON "broadcast [SERVER] Rebooting for upgrade..."                

                Stop-Process -Id $process.ProcessId -Force                
            }

            Write-Host "Updating..."
            New-Item "$($ServerCache)\ShooterGame\Saved" -ItemType Directory -Force
            New-Item "$($ServerCache)\ShooterGame\Content\Mods" -ItemType Directory -Force
            & robocopy "$($ServerCache)" "$($InstallDirectory)" /E /XD ShooterGame\Saved ShooterGame\Content\Mods steamapps /XF UpdateCache.CMD
            Write-Host "Update complete.  Restarting..."
            
            runas /trustlevel:0x20000 "$InstallDirectory\ShooterGame\Saved\Config\WindowsServer\RunServer.cmd"
        }
        else
        {
            Write-Host "No update needed."
        }
    }
    finally
    {
        Remove-Item $updateInProgressFile -Force
    }
}