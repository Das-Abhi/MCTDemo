# ComputerSystem_Metrics.ps1
Write-Output "Collecting Computer System metrics..."
Get-CimInstance Win32_ComputerSystem | Select-Object Name, Manufacturer, Model, TotalPhysicalMemory
Write-Output "Computer System metrics collection completed."