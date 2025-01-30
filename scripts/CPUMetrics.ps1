# CPU_Metrics.ps1
Write-Output "Collecting CPU metrics..."
Get-CimInstance Win32_Processor | Select-Object Name, LoadPercentage, NumberOfCores
Write-Output "CPU metrics collection completed."