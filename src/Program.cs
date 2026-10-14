using System.Globalization;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.VisualBasic.FileIO;

try
{
    if (args.Length != 3) throw new ArgumentException("Usage: <input-folder> <output-folder> <mapping.json>");
    var input = Path.GetFullPath(args[0]);
    var output = Path.GetFullPath(args[1]);
    if (input.Equals(output, StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("Use a separate output folder.");
    var config = JsonSerializer.Deserialize<Config>(File.ReadAllText(args[2])) ?? throw new Exception("Invalid configuration.");
    var files = Directory.GetFiles(input).Where(p => new[]{".csv", ".xlsx"}.Contains(Path.GetExtension(p).ToLowerInvariant())).OrderBy(p => p, StringComparer.Ordinal).ToArray();
    if (files.Length == 0) throw new Exception("No CSV/XLSX input files.");
    var accepted = new List<string[]>();
    var rejected = new List<string[]>();
    var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    int total = 0;
    foreach (var file in files)
    {
        var rows = Read(file);
        if (rows.Count == 0) throw new Exception($"Empty file: {Path.GetFileName(file)}");
        var headers = rows[0].Select(x => x.Trim()).ToArray();
        if (headers.Any(string.IsNullOrWhiteSpace) || headers.Distinct(StringComparer.OrdinalIgnoreCase).Count() != headers.Length) throw new Exception($"Blank/duplicate headers: {file}");
        var indexes = config.Columns.Select(c => Array.FindIndex(headers, h => c.Aliases.Contains(h, StringComparer.OrdinalIgnoreCase))).ToArray();
        if (indexes.Any(i => i < 0)) throw new Exception($"Missing mapped header: {file}");
        for (int r = 1; r < rows.Count; r++)
        {
            total++;
            var raw = rows[r];
            var values = indexes.Select(i => i < raw.Length ? raw[i].Trim() : "").ToArray();
            string? error = raw.Length != headers.Length ? "Column count mismatch" : null;
            for (int c = 0; c < config.Columns.Length; c++)
            {
                var col = config.Columns[c];
                if (col.Required && values[c].Length == 0) error ??= $"Missing {col.Name}";
                if (col.NonnegativeDecimal && (!decimal.TryParse(values[c], NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var n) || n < 0)) error ??= $"Invalid {col.Name}";
            }
            int keyIndex = Array.FindIndex(config.Columns, c => c.Name == config.Key);
            if (keyIndex < 0) throw new Exception("Configured key is not an output column.");
            if (values[keyIndex].Length == 0) error ??= "Empty key";
            if (error == null && !keys.Add(values[keyIndex])) error = "Duplicate key (first valid row wins)";
            if (error == null) accepted.Add(values);
            else rejected.Add(new[]{Path.GetFileName(file), (r + 1).ToString(), error, JsonSerializer.Serialize(raw)});
        }
    }
    Directory.CreateDirectory(output);
    var run = Path.Combine(output, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8]);
    Directory.CreateDirectory(run);
    var names = config.Columns.Select(c => c.Name).ToArray();
    WriteCsv(Path.Combine(run, "clean.csv"), names, accepted);
    WriteCsv(Path.Combine(run, "exceptions.csv"), new[]{"SourceFile","Record","Reason","OriginalValuesJson"}, rejected);
    using (var workbook = new XLWorkbook())
    {
        var sheet = workbook.Worksheets.Add("Clean data");
        var all = new[]{names}.Concat(accepted).ToArray();
        for (int r = 0; r < all.Length; r++) for (int c = 0; c < all[r].Length; c++) sheet.Cell(r+1,c+1).Value = all[r][c];
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);
        workbook.SaveAs(Path.Combine(run,"clean.xlsx"));
    }
    File.WriteAllText(Path.Combine(run,"summary.json"), JsonSerializer.Serialize(new{Files=files.Length,Total=total,Accepted=accepted.Count,Rejected=rejected.Count}, new JsonSerializerOptions{WriteIndented=true}));
    Console.WriteLine(run);
    return 0;
}
catch (Exception e) { Console.Error.WriteLine(e.Message); return 1; }

static List<string[]> Read(string file)
{
    var rows = new List<string[]>();
    if (Path.GetExtension(file).Equals(".csv",StringComparison.OrdinalIgnoreCase))
    {
        using var parser = new TextFieldParser(file, Encoding.UTF8) { TextFieldType=FieldType.Delimited, HasFieldsEnclosedInQuotes=true, TrimWhiteSpace=false };
        parser.SetDelimiters(",");
        while(!parser.EndOfData) rows.Add(parser.ReadFields() ?? Array.Empty<string>());
    }
    else
    {
        using var book = new XLWorkbook(file);
        var sheet = book.Worksheet(1);
        var last = sheet.LastCellUsed();
        if(last == null) return rows;
        for(int r=1;r<=last.Address.RowNumber;r++)
        {
            var cells = new List<string>();
            for(int c=1;c<=last.Address.ColumnNumber;c++)
            {
                var cell=sheet.Cell(r,c);
                if(cell.HasFormula) throw new Exception("Formula inputs are unsupported; export values first.");
                cells.Add(cell.GetString());
            }
            rows.Add(cells.ToArray());
        }
    }
    return rows;
}
static void WriteCsv(string path, string[] header, IEnumerable<string[]> rows)
{
    // Prefix formula-like text for safe spreadsheet opening. XLSX writes text cells.
    static string Escape(string value)
    {
        var trimmed=value.TrimStart();
        if(trimmed.Length>0 && "=+-@".Contains(trimmed[0])) value="'"+value;
        return "\""+value.Replace("\"","\"\"")+"\"";
    }
    using var writer=new StreamWriter(path,false,new UTF8Encoding(true));
    foreach(var row in new[]{header}.Concat(rows)) writer.WriteLine(string.Join(",",row.Select(Escape)));
}
record Column(string Name,string[] Aliases,bool Required=false,bool NonnegativeDecimal=false);
record Config(string Key,Column[] Columns);
