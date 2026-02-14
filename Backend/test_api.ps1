# Bypass SSL certificate validation for older PowerShell versions
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }

try {
    Write-Host "Trying HTTP on correct port 5276..."
    $response = Invoke-RestMethod -Method Post -Uri "http://localhost:5276/api/news/refresh" -ErrorAction Stop
    Write-Host "Response:"
    $response | ConvertTo-Json -Depth 5
}
catch {
    Write-Host "HTTP Error: $_"
    try {
        Write-Host "Trying HTTPS on correct port 7225..."
        $response = Invoke-RestMethod -Method Post -Uri "https://localhost:7225/api/news/refresh" -ErrorAction Stop
        Write-Host "HTTPS Response:"
        $response | ConvertTo-Json -Depth 5
    }
    catch {
        Write-Host "HTTPS Error: $_"
    }
}
