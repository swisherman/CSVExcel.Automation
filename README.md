# CsvExcel.Automation
C#/.NET 10 starter for reusable business CSV/XLSX processing. Windows verification passed October 14, 2026 using .NET SDK 10.0.401: Release build succeeded; CSV acceptance passed (8 rows, 4 accepted, 4 rejected); Excel round-trip passed (4 accepted, 0 rejected). Edge-case testing remains pending.

Read docs/Project-Outline.md for features, roadmap, requirements and acceptance criteria; docs/Fiverr-Gig-Draft.md for the new gig; docs/Upwork-Continuation.md for the parallel larger-project strategy.

From this extracted project folder in PowerShell:
```powershell
dotnet restore .\src\CsvExcel.Automation.csproj
dotnet build .\src\CsvExcel.Automation.csproj -c Release
dotnet run --project .\src\CsvExcel.Automation.csproj -- .\samples\input .\outputs .\samples\mapping.json
.\Verify-Demo.ps1
```
Expected 8 input rows / 4 accepted / 4 rejected. Inspect clean.xlsx manually. The CSV demo is tested by Verify-Demo.ps1; XLSX input needs separate validation before client use.

Publish Windows executable after passing checks:
```powershell
dotnet publish .\src\CsvExcel.Automation.csproj -c Release -r win-x64 --self-contained true -o .\release
```
NuGet internet access required to restore ClosedXML 0.105.1. No service credentials, live integrations or production customer records included. Review dependency notices before distribution.
