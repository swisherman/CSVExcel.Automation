using System.Diagnostics;
using System.Text.Json;
using ClosedXML.Excel;

// Invoked by Verify-ExcelAndConfiguration.ps1 from a temporary project.
if (args.Length != 1) throw new ArgumentException("Pass the application DLL path.");
var app = Path.GetFullPath(args[0]);
var root = Path.Combine(Path.GetTempPath(), "CsvExcel-Extended-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var passed = 0;
var failed = 0;
const string mapping = """
{"Key":"SKU","Columns":[{"Name":"SKU","Aliases":["SKU","ProductCode"],"Required":true},{"Name":"Name","Aliases":["Name","Title"],"Required":true},{"Name":"Price","Aliases":["Price","Cost"],"Required":true,"NonnegativeDecimal":true}]}
""";
try
{
    Check("XLSX text identifiers and quoted names survive both exports", d =>
    {
        Workbook(d, s =>
        {
            Headers(s);
            s.Cell(2, 1).Value = "001";
            s.Cell(2, 2).Value = "Mug, blue";
            s.Cell(2, 3).Value = 12.50;
        });
        var run = Run(d);
        Require(run.Exit == 0, run.Error);
        var folder = Directory.GetDirectories(run.Output).Single();
        using var clean = new XLWorkbook(Path.Combine(folder, "clean.xlsx"));
        var sheet = clean.Worksheet(1);
        Require(sheet.Cell(2, 1).DataType == XLDataType.Text && sheet.Cell(2, 1).GetString() == "001", "Leading-zero SKU lost in XLSX.");
        Require(sheet.Cell(2, 2).GetString() == "Mug, blue", "Product name changed.");
        var csv = File.ReadAllText(Path.Combine(folder, "clean.csv"));
        Require(csv.Contains("\"001\",\"Mug, blue\",\"12.5"), "Expected quoted CSV data missing.");
        using var summary = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "summary.json")));
        Require(summary.RootElement.GetProperty("Total").GetInt32() == 1 && summary.RootElement.GetProperty("Accepted").GetInt32() == 1 && summary.RootElement.GetProperty("Rejected").GetInt32() == 0, "Counts do not reconcile.");
        var second = Run(d);
        Require(second.Exit == 0 && Directory.GetDirectories(second.Output).Length == 2, "Repeat run did not preserve both output folders.");
    });
    Check("Formula cells reject the entire workbook before output", d =>
    {
        Workbook(d, s => { Headers(s); s.Cell(2,1).Value = "001"; s.Cell(2,2).Value = "Mug"; s.Cell(2,3).FormulaA1 = "1+2"; });
        Failure(Run(d), "Formula inputs are unsupported");
    });
    Check("Missing XLSX mapped header", d =>
    {
        Workbook(d, s => { s.Cell(1,1).Value = "SKU"; s.Cell(1,2).Value = "Name"; s.Cell(2,1).Value = "001"; s.Cell(2,2).Value = "Mug"; });
        Failure(Run(d), "Missing mapped header");
    });
    Check("Empty worksheet", d => { Workbook(d, _ => {}); Failure(Run(d), "Empty file:"); });
    Check("Duplicate XLSX headers", d => { Workbook(d, s => { Headers(s); s.Cell(1,2).Value = "sku"; }); Failure(Run(d), "Blank/duplicate headers"); });
    Check("Blank XLSX header", d => { Workbook(d, s => { s.Cell(1,1).Value = "SKU"; s.Cell(1,3).Value = "Price"; }); Failure(Run(d), "Blank/duplicate headers"); });
    BadConfig("Null Columns", "{\"Key\":\"SKU\",\"Columns\":null}", "Configuration Columns must contain");
    BadConfig("Empty Columns", "{\"Key\":\"SKU\",\"Columns\":[]}", "Configuration Columns must contain");
    BadConfig("Null column entry", "{\"Key\":\"SKU\",\"Columns\":[null]}", "cannot contain null");
    BadConfig("Blank Key", mapping.Replace("\"Key\":\"SKU\"", "\"Key\":\" \""), "Key must be nonblank");
    BadConfig("Undefined Key with header-only input", mapping.Replace("\"Key\":\"SKU\"", "\"Key\":\"Missing\""), "Configured key is not an output column");
    BadConfig("Duplicate output names", mapping.Replace("\"Name\":\"Name\"", "\"Name\":\"sku\""), "column names must be unique");
    BadConfig("Blank output name", mapping.Replace("\"Name\":\"Name\"", "\"Name\":\" \""), "column names must be nonblank");
    BadConfig("Null Aliases", mapping.Replace("[\"SKU\",\"ProductCode\"]", "null"), "needs nonblank, trimmed aliases");
    BadConfig("Empty Aliases", mapping.Replace("[\"SKU\",\"ProductCode\"]", "[]"), "needs nonblank, trimmed aliases");
    BadConfig("Blank alias", mapping.Replace("\"ProductCode\"", "\" \""), "needs nonblank, trimmed aliases");
    BadConfig("Duplicate alias", mapping.Replace("\"ProductCode\"", "\"sku\""), "Aliases within an output column must be unique");
    BadConfig("Alias shared by output columns", mapping.Replace("\"Title\"", "\"SKU\""), "cannot map to multiple output columns");
}
finally
{
    Directory.Delete(root, true);
    Console.WriteLine($"Passed: {passed}; Failed: {failed}");
}
return failed == 0 ? 0 : 1;

void Check(string name, Action<string> action)
{
    var folder = Path.Combine(root, Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(folder);
    try { action(folder); passed++; Console.WriteLine($"PASS: {name}"); }
    catch (Exception e) { failed++; Console.WriteLine($"FAIL: {name}: {e.Message}"); }
}
void BadConfig(string name, string config, string error) => Check(name, d =>
{
    var input = Path.Combine(d, "input"); Directory.CreateDirectory(input);
    // Header-only input ensures validation is independent of data rows.
    File.WriteAllText(Path.Combine(input, "test.csv"), "SKU,Name,Price\n");
    Failure(Run(d, config), error);
});
void Workbook(string d, Action<IXLWorksheet> fill)
{
    var input = Path.Combine(d, "input"); Directory.CreateDirectory(input);
    using var book = new XLWorkbook(); var sheet = book.Worksheets.Add("Input");
    fill(sheet); book.SaveAs(Path.Combine(input, "test.xlsx"));
}
void Headers(IXLWorksheet s) { s.Cell(1,1).Value = "SKU"; s.Cell(1,2).Value = "Name"; s.Cell(1,3).Value = "Price"; }
(int Exit, string Error, string Output) Run(string d, string config = mapping)
{
    var configPath = Path.Combine(d, "mapping.json"); File.WriteAllText(configPath, config);
    var input = Path.Combine(d, "input"); var output = Path.Combine(d, "output");
    var psi = new ProcessStartInfo("dotnet") { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false };
    foreach (var arg in new[] { app, input, output, configPath }) psi.ArgumentList.Add(arg);
    using var process = Process.Start(psi) ?? throw new Exception("Could not start dotnet.");
    var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
    if (!process.WaitForExit(60000)) { process.Kill(true); throw new Exception("Application exceeded 60 seconds."); }
    Task.WaitAll(stdout, stderr);
    return (process.ExitCode, stderr.Result + stdout.Result, output);
}
void Failure((int Exit, string Error, string Output) run, string expected)
{
    Require(run.Exit == 1, $"Expected exit 1; got {run.Exit}. {run.Error}");
    Require(run.Error.Contains(expected, StringComparison.OrdinalIgnoreCase), $"Expected '{expected}'. Got: {run.Error}");
    Require(!Directory.Exists(run.Output), "Failure created an output directory.");
}
void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
