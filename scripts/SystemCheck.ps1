# SystemCheck.ps1
Write-Output "Running system check..."
Get-Service | Where-Object {$_.Status -eq "Running"}
Write-Output "System check completed." 