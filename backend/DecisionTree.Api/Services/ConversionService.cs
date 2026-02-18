using DecisionTree.Api.Data;
using DecisionTree.Api.Entities;
using OfficeOpenXml;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace DecisionTree.Api.Services;

/// <summary>
/// Service for converting between Excel and JSON formats
/// Handles bi-directional conversion for decision tree data
/// </summary>
public class ConversionService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ConversionService> _logger;
    private readonly ExcelService _excelService;

    public ConversionService(
        AppDbContext db,
        ILogger<ConversionService> logger,
        ExcelService excelService)
    {
        _db = db;
        _logger = logger;
        _excelService = excelService;
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// Convert uploaded Excel file to JSON format
    /// Returns JSON string that can be imported
    /// </summary>
    public async Task<ConversionResult> ExcelToJsonAsync(
        int decisionTreeId,
        Stream excelStream,
        CancellationToken ct = default)
    {
        var result = new ConversionResult { Success = true };

        try
        {
            // Validate stream
            if (excelStream == null || excelStream.Length == 0)
                throw new InvalidOperationException("Excel file is empty or null");

            // Reset stream position
            if (excelStream.CanSeek)
                excelStream.Seek(0, SeekOrigin.Begin);

            // Load decision tree with tables and columns
            var dt = await _db.DecisionTrees
                .Include(x => x.Tables)
                .ThenInclude(x => x.Columns)
                .FirstOrDefaultAsync(x => x.Id == decisionTreeId, ct);

            if (dt is null)
                throw new InvalidOperationException($"Decision tree with id {decisionTreeId} not found");

            // Check if decision tree has tables
            if (dt.Tables == null || dt.Tables.Count == 0)
                throw new InvalidOperationException("Decision tree has no tables defined");

            // Read Excel file
            var readResult = await _excelService.ReadExcelAsync(excelStream, dt.Tables.ToList(), ct);

            if (!readResult.Success)
            {
                result.Success = false;
                result.Errors = readResult.Errors;
                _logger.LogWarning($"Excel reading failed: {string.Join("; ", readResult.Errors)}");
                return result;
            }

            // Convert Excel data to JSON format
            var jsonData = new Dictionary<string, object>();

            foreach (var tableEntry in readResult.TableData)
            {
                var tableName = tableEntry.Key;
                var tableData = tableEntry.Value;
                var rowsArray = new List<Dictionary<string, object>>();

                _logger.LogInformation($"Converting table '{tableName}' with {tableData.Rows.Count} rows");

                foreach (var row in tableData.Rows)
                {
                    var rowDict = new Dictionary<string, object>();
                    foreach (var kvp in row)
                    {
                        rowDict[kvp.Key] = kvp.Value ?? "";
                    }
                    rowsArray.Add(rowDict);
                }

                jsonData[tableName] = rowsArray;
            }

            // Check if we have data
            if (jsonData.Count == 0)
            {
                result.Success = false;
                result.Errors.Add("No data found in Excel file. Check sheet names match table names.");
                _logger.LogWarning("No data found in Excel file");
                return result;
            }

            // Serialize to JSON string
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            result.JsonContent = JsonSerializer.Serialize(jsonData, options);
            result.Warnings = readResult.Warnings;

            _logger.LogInformation($"Successfully converted Excel to JSON for decision tree {decisionTreeId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting Excel to JSON");
            result.Success = false;
            result.Errors.Add($"Conversion failed: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Convert decision tree data from database to Excel file format
    /// Downloads as .xlsx file
    /// </summary>
    public async Task<ConversionResult> JsonToExcelAsync(
        int decisionTreeId,
        CancellationToken ct = default)
    {
        var result = new ConversionResult { Success = true };

        try
        {
            // Load decision tree with all data
            var dt = await _db.DecisionTrees
                .Include(x => x.Tables)
                .ThenInclude(x => x.Columns)
                .FirstOrDefaultAsync(x => x.Id == decisionTreeId, ct);

            if (dt is null)
                throw new InvalidOperationException($"Decision tree with id {decisionTreeId} not found");

            // Create Excel workbook
            using var package = new ExcelPackage();

            foreach (var table in dt.Tables.Where(t => t.Columns.Any()))
            {
                // Get data rows for this table
                var dataRows = await _db.DecisionTreeData
                    .Where(x => x.TableId == table.Id)
                    .OrderBy(x => x.RowIndex)
                    .ToListAsync(ct);

                if (dataRows.Count == 0)
                    continue;

                // Create worksheet
                var worksheet = package.Workbook.Worksheets.Add(SanitizeSheetName(table.TableName));

                // Get active columns
                var columns = table.Columns
                    .Where(c => c.StatusCode == Entities.StatusCode.Active)
                    .OrderBy(c => c.OrderIndex)
                    .ThenBy(c => c.Id)
                    .ToList();

                // Write headers
                for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                {
                    worksheet.Cells[1, colIdx + 1].Value = columns[colIdx].ColumnName;
                }

                // Write data rows
                for (int rowIdx = 0; rowIdx < dataRows.Count; rowIdx++)
                {
                    var dataRow = dataRows[rowIdx];
                    var rowData = JsonDocument.Parse(dataRow.RowDataJson).RootElement;

                    for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                    {
                        var columnName = columns[colIdx].ColumnName;

                        if (rowData.TryGetProperty(columnName, out var value))
                        {
                            // Convert JSON value to appropriate type based on ValueKind
                            object? cellValue = value.ValueKind switch
                            {
                                JsonValueKind.String => value.GetString() ?? "",
                                JsonValueKind.Number => value.TryGetDouble(out var dbl) ? dbl : value.GetRawText(),
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                JsonValueKind.Null => null,
                                _ => value.GetRawText()
                            };

                            if (cellValue != null)
                            {
                                worksheet.Cells[rowIdx + 2, colIdx + 1].Value = cellValue;
                            }
                        }
                    }
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();
            }

            // Convert to byte array
            result.FileBytes = package.GetAsByteArray();
            result.FileName = $"DecisionTree_{decisionTreeId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            _logger.LogInformation($"Successfully converted JSON to Excel for decision tree {decisionTreeId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting JSON to Excel");
            result.Success = false;
            result.Errors.Add($"Conversion failed: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Convert JSON string to Excel file (user-provided JSON data)
    /// </summary>
    public async Task<ConversionResult> JsonStringToExcelAsync(
        int decisionTreeId,
        string jsonContent,
        CancellationToken ct = default)
    {
        var result = new ConversionResult { Success = true };

        try
        {
            // Validate JSON
            if (string.IsNullOrWhiteSpace(jsonContent))
                throw new InvalidOperationException("JSON content is empty");

            var jsonDoc = JsonDocument.Parse(jsonContent);
            if (jsonDoc.RootElement.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("JSON must be an object");

            // Load decision tree structure
            var dt = await _db.DecisionTrees
                .Include(x => x.Tables)
                .ThenInclude(x => x.Columns)
                .FirstOrDefaultAsync(x => x.Id == decisionTreeId, ct);

            if (dt is null)
                throw new InvalidOperationException($"Decision tree with id {decisionTreeId} not found");

            // Create Excel workbook
            using var package = new ExcelPackage();
            var worksheetCount = 0;

            foreach (var table in dt.Tables.Where(t => t.Columns.Any()))
            {
                // Get active columns
                var columns = table.Columns
                    .Where(c => c.StatusCode == Entities.StatusCode.Active)
                    .OrderBy(c => c.OrderIndex)
                    .ThenBy(c => c.Id)
                    .ToList();

                if (columns.Count == 0)
                    continue;

                // Resolve rows from supported JSON formats:
                // 1) { "TableName": [ {col:val}, ... ] }
                // 2) { "inputs": [ { name, value: { metadata, data } } ] } (and outputs)
                // 3) { "metadata": ..., "inputs": [...], "outputs": [...] }
                if (!TryResolveRowsForTable(jsonDoc.RootElement, table.TableName, columns, out var rows))
                    continue;

                if (rows.Count == 0)
                    continue;

                // Create worksheet
                var worksheet = package.Workbook.Worksheets.Add(SanitizeSheetName(table.TableName));
                worksheetCount++;

                // Write headers
                for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                {
                    worksheet.Cells[1, colIdx + 1].Value = columns[colIdx].ColumnName;
                }

                // Write data rows
                int rowIndex = 2;
                foreach (var row in rows)
                {
                    for (int colIdx = 0; colIdx < columns.Count; colIdx++)
                    {
                        var columnName = columns[colIdx].ColumnName;

                        if (row.TryGetValue(columnName, out var value))
                        {
                            if (value != null)
                            {
                                worksheet.Cells[rowIndex, colIdx + 1].Value = value;
                            }
                        }
                    }
                    rowIndex++;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();
            }

            // Fallback: If decision-tree schema matching produced no sheet,
            // still try generic JSON->Excel conversion from payload itself.
            if (worksheetCount == 0)
            {
                worksheetCount += BuildWorksheetsFromJsonPayload(package, jsonDoc.RootElement);
            }

            if (worksheetCount == 0)
                throw new InvalidOperationException(
                    "JSON format does not contain convertible table data. Supported formats: " +
                    "{ \"TableName\": [ ... ] } or { \"inputs\": [ { \"name\": \"TableName\", \"value\": { \"metadata\": [...], \"data\": [...] } } ] }");

            // Convert to byte array
            result.FileBytes = package.GetAsByteArray();
            result.FileName = $"DecisionTree_{decisionTreeId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            _logger.LogInformation($"Successfully converted JSON string to Excel for decision tree {decisionTreeId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting JSON string to Excel");
            result.Success = false;
            result.Errors.Add($"Conversion failed: {ex.Message}");
        }

        return result;
    }

    private static bool TryResolveRowsForTable(
        JsonElement root,
        string tableName,
        List<TableColumn> columns,
        out List<Dictionary<string, object?>> rows)
    {
        rows = new List<Dictionary<string, object?>>();

        // Format 1: { "TableName": [ {col:val}, ... ] }
        if (TryGetPropertyIgnoreCase(root, tableName, out var directTableData) &&
            directTableData.ValueKind == JsonValueKind.Array)
        {
            foreach (var rowElement in directTableData.EnumerateArray())
            {
                if (rowElement.ValueKind != JsonValueKind.Object)
                    continue;

                var rowDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in rowElement.EnumerateObject())
                {
                    rowDict[prop.Name] = ConvertJsonElementToObject(prop.Value);
                }
                rows.Add(rowDict);
            }

            return true;
        }

        // Format 2/3: { "inputs": [...], "outputs": [...] } wrappers
        foreach (var sectionName in new[] { "inputs", "outputs" })
        {
            if (!TryGetPropertyIgnoreCase(root, sectionName, out var sectionElement) ||
                sectionElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var wrapper in sectionElement.EnumerateArray())
            {
                if (wrapper.ValueKind != JsonValueKind.Object)
                    continue;

                if (!TryGetPropertyIgnoreCase(wrapper, "name", out var nameElement))
                    continue;

                var wrapperName = nameElement.GetString();
                if (!string.Equals(wrapperName, tableName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!TryGetPropertyIgnoreCase(wrapper, "value", out var valueElement) ||
                    valueElement.ValueKind != JsonValueKind.Object)
                {
                    return true;
                }

                if (!TryGetPropertyIgnoreCase(valueElement, "data", out var dataElement) ||
                    dataElement.ValueKind != JsonValueKind.Array)
                {
                    return true;
                }

                foreach (var rowArray in dataElement.EnumerateArray())
                {
                    if (rowArray.ValueKind == JsonValueKind.Object)
                    {
                        var rowDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        foreach (var prop in rowArray.EnumerateObject())
                        {
                            rowDict[prop.Name] = ConvertJsonElementToObject(prop.Value);
                        }
                        rows.Add(rowDict);
                        continue;
                    }

                    if (rowArray.ValueKind != JsonValueKind.Array)
                        continue;

                    var values = rowArray.EnumerateArray().ToList();
                    var rowDictByOrder = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

                    for (int i = 0; i < columns.Count && i < values.Count; i++)
                    {
                        rowDictByOrder[columns[i].ColumnName] = ConvertJsonElementToObject(values[i]);
                    }

                    rows.Add(rowDictByOrder);
                }

                return true;
            }
        }

        return false;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (string.Equals(prop.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static object? ConvertJsonElementToObject(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt64(out var i) ? i : value.TryGetDouble(out var d) ? d : value.GetRawText(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value.GetRawText()
        };
    }

    private static int BuildWorksheetsFromJsonPayload(ExcelPackage package, JsonElement root)
    {
        var created = 0;

        // Format: { inputs: [...], outputs: [...] }
        foreach (var sectionName in new[] { "inputs", "outputs" })
        {
            if (!TryGetPropertyIgnoreCase(root, sectionName, out var section) || section.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var wrapper in section.EnumerateArray())
            {
                if (wrapper.ValueKind != JsonValueKind.Object)
                    continue;

                if (!TryGetPropertyIgnoreCase(wrapper, "name", out var nameEl))
                    continue;

                var sheetName = nameEl.GetString();
                if (string.IsNullOrWhiteSpace(sheetName))
                    continue;

                if (!TryGetPropertyIgnoreCase(wrapper, "value", out var valueEl) || valueEl.ValueKind != JsonValueKind.Object)
                    continue;

                if (!TryGetPropertyIgnoreCase(valueEl, "data", out var dataEl) || dataEl.ValueKind != JsonValueKind.Array)
                    continue;

                var headers = ExtractHeadersFromValue(valueEl, dataEl);
                if (headers.Count == 0)
                    continue;

                var ws = package.Workbook.Worksheets.Add(SanitizeSheetName(sheetName));
                WriteHeaders(ws, headers);
                WriteDataRows(ws, dataEl, headers);
                ws.Cells.AutoFitColumns();
                created++;
            }
        }

        // Format: { "TableName": [ ... ] }
        if (created == 0 && root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
            {
                if (prop.Value.ValueKind != JsonValueKind.Array)
                    continue;

                var headers = ExtractHeadersFromArray(prop.Value);
                if (headers.Count == 0)
                    continue;

                var ws = package.Workbook.Worksheets.Add(SanitizeSheetName(prop.Name));
                WriteHeaders(ws, headers);
                WriteDataRows(ws, prop.Value, headers);
                ws.Cells.AutoFitColumns();
                created++;
            }
        }

        return created;
    }

    private static List<string> ExtractHeadersFromValue(JsonElement valueElement, JsonElement dataElement)
    {
        // Preferred source: metadata array [{ ColumnName: "type" }, ...]
        if (TryGetPropertyIgnoreCase(valueElement, "metadata", out var metadataEl) &&
            metadataEl.ValueKind == JsonValueKind.Array)
        {
            var headers = new List<string>();
            foreach (var md in metadataEl.EnumerateArray())
            {
                if (md.ValueKind != JsonValueKind.Object)
                    continue;

                var first = md.EnumerateObject().FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(first.Name))
                    headers.Add(first.Name);
            }

            if (headers.Count > 0)
                return headers;
        }

        return ExtractHeadersFromArray(dataElement);
    }

    private static List<string> ExtractHeadersFromArray(JsonElement dataArray)
    {
        if (dataArray.ValueKind != JsonValueKind.Array)
            return new List<string>();

        var firstRow = dataArray.EnumerateArray().FirstOrDefault();
        if (firstRow.ValueKind == JsonValueKind.Object)
        {
            return firstRow.EnumerateObject().Select(p => p.Name).ToList();
        }

        if (firstRow.ValueKind == JsonValueKind.Array)
        {
            var count = firstRow.GetArrayLength();
            var headers = new List<string>(count);
            for (int i = 1; i <= count; i++)
            {
                headers.Add($"Column{i}");
            }
            return headers;
        }

        return new List<string>();
    }

    private static void WriteHeaders(ExcelWorksheet worksheet, List<string> headers)
    {
        for (int col = 0; col < headers.Count; col++)
        {
            worksheet.Cells[1, col + 1].Value = headers[col];
        }
    }

    private static void WriteDataRows(ExcelWorksheet worksheet, JsonElement dataArray, List<string> headers)
    {
        int rowIndex = 2;
        foreach (var row in dataArray.EnumerateArray())
        {
            if (row.ValueKind == JsonValueKind.Object)
            {
                for (int col = 0; col < headers.Count; col++)
                {
                    if (TryGetPropertyIgnoreCase(row, headers[col], out var value))
                    {
                        worksheet.Cells[rowIndex, col + 1].Value = ConvertJsonElementToObject(value);
                    }
                }
            }
            else if (row.ValueKind == JsonValueKind.Array)
            {
                var values = row.EnumerateArray().ToList();
                for (int col = 0; col < headers.Count && col < values.Count; col++)
                {
                    worksheet.Cells[rowIndex, col + 1].Value = ConvertJsonElementToObject(values[col]);
                }
            }

            rowIndex++;
        }
    }

    /// <summary>
    /// Sanitize worksheet name for Excel (max 31 chars, no special chars)
    /// </summary>
    private static string SanitizeSheetName(string name)
    {
        var invalidChars = new[] { '/', '\\', '?', '*', '[', ']' };
        var sanitized = new string(name.Where(c => !invalidChars.Contains(c)).ToArray());
        return sanitized.Length > 31 ? sanitized[..31] : sanitized;
    }
}

/// <summary>
/// Result object for conversion operations
/// </summary>
public class ConversionResult
{
    public bool Success { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    // For Excel to JSON conversion
    public string JsonContent { get; set; } = string.Empty;
    
    // For JSON to Excel conversion
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
}
