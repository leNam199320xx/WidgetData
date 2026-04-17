$r = Invoke-RestMethod -Uri "http://localhost:5114/api/auth/login" -Method POST -Body '{"email":"admin@widgetdata.com","password":"Admin@123!"}' -ContentType "application/json" -UseBasicParsing
$token = $r.token
$h = @{Authorization="Bearer $token"}

Write-Host "=== 1. Execute Widget 1 ==="
try {
    $exec = Invoke-RestMethod -Uri "http://localhost:5114/api/widgets/1/execute" -Method POST -Headers $h -UseBasicParsing
    Write-Host "OK: status=$($exec.status) rows=$($exec.rowCount) ms=$($exec.executionTimeMs)"
} catch { Write-Host "ERROR: $($_.Exception.Response.StatusCode) - $_" }

Write-Host "=== 2. Get Data Widget 1 ==="
try {
    $data = Invoke-RestMethod -Uri "http://localhost:5114/api/widgets/1/data" -Headers $h -UseBasicParsing
    Write-Host "OK: $($data | ConvertTo-Json -Compress -Depth 3)"
} catch { Write-Host "ERROR: $($_.Exception.Response.StatusCode) - $_" }

Write-Host "=== 3. Test DataSource 1 Connection ==="
try {
    $dsTest = Invoke-RestMethod -Uri "http://localhost:5114/api/datasources/1/test" -Method POST -Headers $h -UseBasicParsing
    Write-Host "OK: $($dsTest | ConvertTo-Json -Compress)"
} catch { Write-Host "ERROR: $($_.Exception.Response.StatusCode)" }

Write-Host "=== 4. List Schedules ==="
try {
    $sched = Invoke-RestMethod -Uri "http://localhost:5114/api/schedules" -Headers $h -UseBasicParsing
    Write-Host "OK: $($sched.total) schedules"
} catch { Write-Host "ERROR: $($_.Exception.Response.StatusCode)" }

Write-Host "=== 5. Trigger Schedule 1 ==="
try {
    $trig = Invoke-RestMethod -Uri "http://localhost:5114/api/schedules/1/trigger" -Method POST -Headers $h -UseBasicParsing
    Write-Host "OK: $($trig | ConvertTo-Json -Compress)"
} catch { Write-Host "ERROR: $($_.Exception.Response.StatusCode)" }

Write-Host "=== 6. Manager login + try Delete Schedule (expect 403) ==="
$rm = Invoke-RestMethod -Uri "http://localhost:5114/api/auth/login" -Method POST -Body '{"email":"manager@widgetdata.com","password":"Manager@123!"}' -ContentType "application/json" -UseBasicParsing
$hm = @{Authorization="Bearer $($rm.token)"}
try {
    Invoke-RestMethod -Uri "http://localhost:5114/api/schedules/1" -Method DELETE -Headers $hm -UseBasicParsing | Out-Null
    Write-Host "ERROR: Should have been 403!"
} catch {
    $code = $_.Exception.Response.StatusCode
    if ($code -eq "Forbidden") { Write-Host "OK: 403 Forbidden as expected" }
    else { Write-Host "UNEXPECTED: $code" }
}

Write-Host "=== 7. Export Widget 1 as CSV ==="
try {
    $csv = Invoke-RestMethod -Uri "http://localhost:5114/api/exports?widgetId=1&format=csv" -Method POST -Headers $h -UseBasicParsing
    Write-Host "CSV length: $($csv.Length) chars"
} catch { Write-Host "ERROR: $($_.Exception.Response.StatusCode) - $_" }

Write-Host "=== DONE ==="
