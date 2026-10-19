$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repo 'src\CsvExcel.Automation.csproj'
$testSource = Join-Path $PSScriptRoot 'ExcelConfigurationChecks.cs'
$tempProject = Join-Path ([System.IO.Path]::GetTempPath()) ('CsvExcel-CheckHost-' + [guid]::NewGuid().ToString('N'))

try {
    & dotnet build $project -c Release
    if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
    $dll = Join-Path $repo 'src\bin\Release\net10.0\CsvExcel.Automation.dll'
    New-Item -ItemType Directory -Path $tempProject | Out-Null
    # Reuse the application project dependency version; no Excel COM automation.
    @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup>
  <ItemGroup><PackageReference Include="ClosedXML" Version="0.105.1" /></ItemGroup>
</Project>
'@ | Set-Content -LiteralPath (Join-Path $tempProject 'Checks.csproj') -Encoding UTF8
    Copy-Item -LiteralPath $testSource -Destination (Join-Path $tempProject 'Program.cs')
    & dotnet run --project (Join-Path $tempProject 'Checks.csproj') -c Release -- $dll
    if ($LASTEXITCODE -ne 0) { throw 'Excel/configuration checks failed. Review the FAIL lines above.' }
}
finally {
    if (Test-Path -LiteralPath $tempProject) {
        Remove-Item -LiteralPath $tempProject -Recurse -Force
    }
}
