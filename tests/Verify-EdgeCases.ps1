
$ErrorActionPreference = "Stop"

$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo "src\CsvExcel.Automation.csproj"
$mapping = Join-Path $repo "samples\mapping.json"
$testRoot = Join-Path $env:TEMP ("CsvExcel-Tests-" + [guid]::NewGuid().ToString("N"))

$passed = 0
$failed = 0

function Invoke-EdgeCase {
    param(
    [string]$Name,
    [string]$InputFolder,
    [string]$ExpectedError,
    [string]$MappingFile = $mapping
    )

    $output = Join-Path $testRoot ([guid]::NewGuid().ToString("N"))

    $messages = @()

try {
    $ErrorActionPreference = "Continue"

    $messages = @(
        & dotnet run --project $project -- $InputFolder $output $MappingFile 2>&1 |
            ForEach-Object { $_.ToString() }
    )

    $exitCode = $LASTEXITCODE
}
finally {
    $ErrorActionPreference = "Stop"
}
    $actualMessage = $messages -join "`n"

    $success = (
        $exitCode -eq 1 -and
        $actualMessage.Contains($ExpectedError) -and
        -not (Test-Path -LiteralPath $output)
    )

    if ($success) {
        $script:passed++
        Write-Host "PASS: $Name" -ForegroundColor Green
    }
    else {
        $script:failed++
        Write-Host "FAIL: $Name" -ForegroundColor Red
        Write-Host "Exit code: $exitCode"
        Write-Host $actualMessage
    }
}

try {
    New-Item -ItemType Directory -Path $testRoot | Out-Null

    # Test 1: Empty input directory
    $empty = Join-Path $testRoot "empty-input"
    New-Item -ItemType Directory -Path $empty | Out-Null

    Invoke-EdgeCase `
        -Name "Empty input directory" `
        -InputFolder $empty `
        -ExpectedError "No CSV/XLSX input files."

    # Test 2: Missing mapped header
    $missing = Join-Path $testRoot "missing-header"
    New-Item -ItemType Directory -Path $missing | Out-Null

    @'
SKU,Description
001,Test product
'@ | Set-Content -LiteralPath (Join-Path $missing "test.csv") -Encoding UTF8

    Invoke-EdgeCase `
        -Name "Missing mapped header" `
        -InputFolder $missing `
        -ExpectedError "Missing mapped header"



    # Test 3: Valid and rejected records
    $invalid = Join-Path $testRoot "invalid-records"
    New-Item -ItemType Directory -Path $invalid | Out-Null

    @'
SKU,Name,Price
001,Valid mug,12.50
002,Negative price,-5.00
001,Duplicate mug,15.00
003,,20.00
'@ | Set-Content -LiteralPath (Join-Path $invalid "test.csv") -Encoding UTF8

    $output3 = Join-Path $testRoot "output-invalid-records"

    $messages = @(
        & dotnet run --project $project -- $invalid $output3 $mapping 2>&1 |
            ForEach-Object { $_.ToString() }
    )

    $exitCode = $LASTEXITCODE
    $success = $false

    if ($exitCode -eq 0 -and (Test-Path -LiteralPath $output3)) {
        $run = Get-ChildItem -LiteralPath $output3 -Directory |
            Select-Object -First 1

        if ($null -ne $run) {
            $summary = Get-Content (Join-Path $run.FullName "summary.json") -Raw |
                ConvertFrom-Json

            $accepted = @(Import-Csv (Join-Path $run.FullName "clean.csv"))
            $rejected = @(Import-Csv (Join-Path $run.FullName "exceptions.csv"))

            $success = (
                $summary.Total -eq 4 -and
                $summary.Accepted -eq 1 -and
                $summary.Rejected -eq 3 -and
                $accepted.Count -eq 1 -and
                $accepted[0].SKU -eq "001" -and
                $rejected.Count -eq 3 -and
                $rejected[0].Reason -eq "Invalid Price" -and
                $rejected[1].Reason -eq "Duplicate key (first valid row wins)" -and
                $rejected[2].Reason -eq "Missing Name"
            )
        }
    }

    if ($success) {
        $passed++
        Write-Host "PASS: Invalid records" -ForegroundColor Green
    }
    else {
        $failed++
        Write-Host "FAIL: Invalid records" -ForegroundColor Red
        $messages | ForEach-Object { Write-Host $_ }
    }

    # Test 4: Malformed CSV quotation
    $malformed = Join-Path $testRoot "malformed-csv"
    New-Item -ItemType Directory -Path $malformed | Out-Null

    @'
SKU,Name,Price
001,"Unclosed product name,12.50
'@ | Set-Content -LiteralPath (Join-Path $malformed "test.csv") -Encoding UTF8

    Invoke-EdgeCase `
        -Name "Malformed CSV quotation" `
        -InputFolder $malformed `
        -ExpectedError "cannot be parsed"


    # Test 5: Invalid JSON configuration
    $badJson = Join-Path $testRoot "invalid-mapping.json"

    @'
{
  "Key": "SKU",
  "Columns": [
    {"Name": "SKU", "Aliases": ["SKU"]},
'@ | Set-Content -LiteralPath $badJson -Encoding UTF8

    Invoke-EdgeCase `
        -Name "Invalid JSON configuration" `
        -InputFolder (Join-Path $repo "samples\input") `
        -MappingFile $badJson `
        -ExpectedError "Path:"


    # Test 6: Undefined configuration key
    $badKey = Join-Path $testRoot "invalid-key-mapping.json"

    $config = Get-Content -LiteralPath $mapping -Raw |
        ConvertFrom-Json

    $config.Key = "ProductId"

    $config |
        ConvertTo-Json -Depth 10 |
        Set-Content -LiteralPath $badKey -Encoding UTF8

    Invoke-EdgeCase `
        -Name "Undefined configuration key" `
        -InputFolder (Join-Path $repo "samples\input") `
        -MappingFile $badKey `
        -ExpectedError "Configured key is not an output column."


}
finally {
    Write-Host ""
    Write-Host "Passed: $passed"
    Write-Host "Failed: $failed"

    if (Test-Path -LiteralPath $testRoot) {
        Remove-Item -LiteralPath $testRoot -Recurse -Force
    }
}

if ($failed -gt 0) {
    throw "$failed edge-case tests failed."
}
