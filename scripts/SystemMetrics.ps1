# System_Metrics.ps1
Write-Output "Collecting System metrics..."
Get-CimInstance Win32_ComputerSystem | Select-Object Name, Domain, Workgroup, UserName
Write-Output "System metrics collection completed."