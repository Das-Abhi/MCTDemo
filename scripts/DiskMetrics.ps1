# Disk_Metrics.ps1
Write-Output "Collecting Disk metrics..."
Get-CimInstance Win32_LogicalDisk | Select-Object DeviceID, VolumeName, Size, FreeSpace
Write-Output "Disk metrics collection completed."