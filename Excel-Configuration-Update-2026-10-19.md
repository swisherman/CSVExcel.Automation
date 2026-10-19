# Excel and configuration checks — October 19, 2026

Prepared from the Google Drive copy of CsvExcel.Automation. This update has not been compiled or executed here: this environment has neither dotnet nor PowerShell. Windows execution is required before declaring any new checks passed.

Included changes:
- src/Program.cs: validate mapping structure and key before reading input rows; reject null/empty columns, null entries, blank or duplicate names, invalid aliases, and aliases shared across output columns.
- tests/ExcelConfigurationChecks.cs: 18 scenarios covering Excel round-trip/leading-zero identifiers, repeated runs, formula rejection, empty sheets, missing/blank/duplicate headers, and malformed mapping structures using header-only inputs.
- tests/Verify-ExcelAndConfiguration.ps1: builds the application and runs a temporary C# test host using the same ClosedXML version; requires .NET 10 and NuGet access, but not Excel installation. Temporary fixtures are removed afterward.

Existing Verify-EdgeCases.ps1 is unchanged and is not included in this update ZIP.

Extract these files into E:\repos\CsvExcel.Automation after reviewing git status. Then run:

```powershell
.\tests\Verify-EdgeCases.ps1
.\tests\Verify-ExcelAndConfiguration.ps1
git diff --check
git diff --stat
```

Expected extended suite on success: Passed: 18; Failed: 0. This is an expected result, not an observed test run. Do not publish until both suites pass and packaging/demo checks are complete.
