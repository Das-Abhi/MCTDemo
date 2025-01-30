# OS_Metrics.ps1
Write-Output "Collecting Operating System metrics..."
Get-CimInstance Win32_OperatingSystem | Select-Object Caption, Version, BuildNumber, LastBootUpTime
Write-Output "Operating System metrics collection completed."
