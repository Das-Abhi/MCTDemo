# SystemCheck.ps1
Write-Output "Running system check..."
Get-Service | Where-Object {$_.Status -eq "Running"} | Select-Object -First 5
Write-Output "System check completed."
