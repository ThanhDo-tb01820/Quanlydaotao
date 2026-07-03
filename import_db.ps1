# Import toan bo du lieu tu file SQL vao database QLDAOTAO
$connStr = "Server=localhost;Database=QLDAOTAO;Integrated Security=True;Encrypt=False"
$sqlFile = "import_data.sql"

$conn = New-Object System.Data.SqlClient.SqlConnection($connStr)
$conn.Open()

$sql = Get-Content $sqlFile -Raw -Encoding UTF8

# Tach tung lenh INSERT
$lines = $sql -split "`r?`n" | Where-Object { $_.Trim().StartsWith("INSERT INTO") }

$counts = @{}
$errors = 0
$total = 0

foreach ($line in $lines) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    try {
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = $line.Trim()
        $cmd.ExecuteNonQuery() | Out-Null
        
        # Lay ten bang
        if ($line -match "INSERT INTO (\w+)") {
            $tbl = $matches[1]
            if (-not $counts[$tbl]) { $counts[$tbl] = 0 }
            $counts[$tbl]++
        }
        $total++
    } catch {
        $errors++
        if ($errors -le 5) {
            Write-Host "ERROR: $_" -ForegroundColor Red
            Write-Host "  SQL: $($line.Substring(0, [Math]::Min(100, $line.Length)))" -ForegroundColor Yellow
        }
    }
}

$conn.Close()

Write-Host "`n=== KET QUA IMPORT ===" -ForegroundColor Cyan
foreach ($k in $counts.Keys | Sort-Object) {
    Write-Host "  $k`: $($counts[$k]) rows" -ForegroundColor Green
}
Write-Host "  Tong: $total rows" -ForegroundColor Green
Write-Host "  Loi: $errors" -ForegroundColor $(if($errors -gt 0){"Red"}else{"Green"})
