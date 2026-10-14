$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    $testOutput = Join-Path $PSScriptRoot ('verification-' + [guid]::NewGuid().ToString('N'))
    dotnet run --project .\src\CsvExcel.Automation.csproj -- .\samples\input $testOutput .\samples\mapping.json
    if ($LASTEXITCODE -ne 0) { throw 'Demo run failed.' }
    $run = Get-ChildItem -LiteralPath $testOutput -Directory | Select-Object -First 1
    $summary = Get-Content (Join-Path $run.FullName 'summary.json') -Raw | ConvertFrom-Json
    if ($summary.Total -ne 8 -or $summary.Accepted -ne 4 -or $summary.Rejected -ne 4) { throw 'Row count mismatch.' }
    $rows = @(Import-Csv (Join-Path $run.FullName 'clean.csv'))
    if (($rows.SKU -join ',') -ne '001,002,003,005') { throw 'SKU preservation/order mismatch.' }
    if ($rows[0].Name -ne 'Mug, blue') { throw 'Quoted comma did not survive.' }
    $errors = @(Import-Csv (Join-Path $run.FullName 'exceptions.csv'))
    if ($errors.Count -ne 4) { throw 'Exception count mismatch.' }
    Write-Host 'CSV demo acceptance passed. Review clean.xlsx and test XLSX inputs separately.'
} finally { Pop-Location }
