# Network_Metrics.ps1
Write-Output "Collecting Network Interface metrics..."
Get-CimInstance Win32_NetworkAdapterConfiguration | Where-Object {$_.IPEnabled -eq $true} | Select-Object Description, MACAddress, IPAddress, DefaultIPGateway
Write-Output "Network Interface metrics collection completed."