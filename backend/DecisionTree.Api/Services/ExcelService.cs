using DecisionTree.Api.Entities;
using OfficeOpenXml;
using System.Globalization;
using EntityStatusCode = DecisionTree.Api.Entities.StatusCode;

namespace DecisionTree.Api.Services;

/// <summary>
/// Service for Excel file operations - reading and writing
/// Uses EPPlus library for Excel manipulation
/// </summary>
public class ExcelService
{
    private readonly ILogger<ExcelService> _logger;

    public ExcelService(ILogger<ExcelService> logger)
    {
        _logger = logger;
        // EPPlus requires license context
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }

    /// <summary>
    /// Read Excel file and convert to structured data
    /// Each worksheet = one table
    /// First row = column headers (must match ColumnName or ExcelHeaderName from metadata)
    /// </summary>
    public async Task<ExcelReadResult> ReadExcelAsync(
        Stream excelStream,
        List<DecisionTreeTable> tables,
        CancellationToken ct = default)
    {
        var result = new ExcelReadResult { Success = true };

        try
        {
            using var package = new ExcelPackage(excelStream);

            foreach (var table in tables)
            {
                // Find worksheet by table name
                var worksheet = package.Workbook.Worksheets[table.TableName];

                if (worksheet == null)
                {
                    result.Warnings.Add($"Worksheet '{table.TableName}' not found in Excel file");
                    continue;
                }

                var tableData = ReadWorksheet(worksheet, table);
                
                // 6.2.1: Check for schema validation errors and stop if found
                if (tableData.Errors.Any())
                {
                    result.Success = false;
                    result.Errors.AddRange(tableData.Errors);
                    _logger.LogError($"Schema validation failed for table '{table.TableName}'. Errors: {string.Join("; ", tableData.Errors)}");
                    continue;
                }
                
                // Add warnings from this table
                result.Warnings.AddRange(tableData.Warnings);
                
                if (tableData.Rows.Count > 0)
                {
                    result.TableData[table.TableName] = tableData;
                }
            }

            if (result.TableData.Count == 0)
            {
                result.Warnings.Add("No data found in any worksheet");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading Excel file");
            result.Success = false;
            result.Errors.Add($"Failed to read Excel file: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Read a single worksheet and map to table columns
    /// 6.2.1: Validates schema before reading data
    /// 6.2.2: Uses unique identifier for row matching
    /// </summary>
    private TableDataResult ReadWorksheet(ExcelWorksheet worksheet, DecisionTreeTable table)
    {
        var result = new TableDataResult();

        if (worksheet.Dimension == null || worksheet.Dimension.Rows < 2)
        {
            return result; // Empty or only headers
        }

        // Read header row (row 1)
        var headers = new Dictionary<int, string>(); // columnIndex -> headerName
        for (int col = 1; col <= worksheet.Dimension.Columns; col++)
        {
            var headerValue = worksheet.Cells[1, col].Text?.Trim();
            if (!string.IsNullOrWhiteSpace(headerValue))
            {
                headers[col] = headerValue;
            }
        }

        // Create column mapping: header -> column definition
        var columnMap = new Dictionary<string, TableColumn>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var column in table.Columns.Where(c => c.StatusCode == EntityStatusCode.Active))
        {
            // Try both ColumnName and ExcelHeaderName
            columnMap[column.ColumnName] = column;
            
            if (!string.IsNullOrWhiteSpace(column.ExcelHeaderName))
            {
                columnMap[column.ExcelHeaderName] = column;
            }
        }

        // 6.2.1: SCHEMA VALIDATION - Check if Excel columns match database schema
        var schemaValidation = ValidateSchema(headers, columnMap, table);
        if (!schemaValidation.IsValid)
        {
            result.Errors.AddRange(schemaValidation.Errors);
            return result; // Stop processing if schema is invalid
        }
        
        // Add schema validation warnings
        result.Warnings.AddRange(schemaValidation.Warnings);

        // 6.2.2: Get unique identifier column if exists
        var uniqueIdentifierColumn = table.Columns
            .FirstOrDefault(c => c.IsUniqueIdentifier && c.StatusCode == EntityStatusCode.Active);
        
        if (uniqueIdentifierColumn != null)
        {
            result.UniqueIdentifierColumnName = uniqueIdentifierColumn.ColumnName;
        }

        // Read data rows (starting from row 2)
        for (int row = 2; row <= worksheet.Dimension.Rows; row++)
        {
            var rowData = new Dictionary<string, object?>();
            var rowErrors = new List<string>();
            bool hasAnyData = false;

            foreach (var (colIndex, headerName) in headers)
            {
                // Find matching column definition
                if (!columnMap.TryGetValue(headerName, out var columnDef))
                {
                    continue; // Skip unknown columns
                }

                var cellValue = worksheet.Cells[row, colIndex].Text;
                
                if (string.IsNullOrWhiteSpace(cellValue))
                {
                    if (columnDef.IsRequired)
                    {
                        rowErrors.Add($"Row {row}: Required column '{columnDef.ColumnName}' is empty");
                    }
                    rowData[columnDef.ColumnName] = null;
                    continue;
                }

                hasAnyData = true;

                // Convert value based on data type
                var (convertedValue, error) = ConvertCellValue(cellValue, columnDef, row);
                
                if (error != null)
                {
                    rowErrors.Add(error);
                }

                rowData[columnDef.ColumnName] = convertedValue;
            }

            // Skip completely empty rows
            if (!hasAnyData)
            {
                continue;
            }

            if (rowErrors.Any())
            {
                result.Errors.AddRange(rowErrors);
            }

            result.Rows.Add(rowData);
        }

        return result;
    }

    /// <summary>
    /// Convert cell value to appropriate .NET type based on column data type
    /// </summary>
    private (object? value, string? error) ConvertCellValue(
        string cellValue,
        TableColumn column,
        int rowNumber)
    {
        try
        {
            return column.DataType switch
            {
                ColumnDataType.String => (cellValue, null),
                
                ColumnDataType.Int => int.TryParse(cellValue, out var intVal)
                    ? (intVal, null)
                    : (null, $"Row {rowNumber}, Column '{column.ColumnName}': '{cellValue}' is not a valid integer"),
                
                ColumnDataType.Decimal => decimal.TryParse(cellValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var decVal)
                    ? (decVal, null)
                    : (null, $"Row {rowNumber}, Column '{column.ColumnName}': '{cellValue}' is not a valid decimal"),
                
                ColumnDataType.Date => TryParseDate(cellValue, column.Format, out var dateVal)
                    ? (dateVal, null)
                    : (null, $"Row {rowNumber}, Column '{column.ColumnName}': '{cellValue}' is not a valid date"),
                
                ColumnDataType.Boolean => TryParseBoolean(cellValue, out var boolVal)
                    ? (boolVal, null)
                    : (null, $"Row {rowNumber}, Column '{column.ColumnName}': '{cellValue}' is not a valid boolean"),
                
                _ => (cellValue, null)
            };
        }
        catch (Exception ex)
        {
            return (null, $"Row {rowNumber}, Column '{column.ColumnName}': Conversion error - {ex.Message}");
        }
    }

    /// <summary>
    /// Try to parse date with multiple formats
    /// </summary>
    private bool TryParseDate(string value, string? formatHint, out DateTime result)
    {
        // Try with format hint first
        if (!string.IsNullOrWhiteSpace(formatHint))
        {
            if (DateTime.TryParseExact(value, formatHint, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return true;
            }
        }

        // Try common formats
        var formats = new[]
        {
            "dd/MM/yyyy", "dd.MM.yyyy", "yyyy-MM-dd",
            "dd/MM/yyyy HH:mm:ss", "dd.MM.yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss",
            "MM/dd/yyyy", "MM-dd-yyyy"
        };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(value, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return true;
            }
        }

        // Try general parse as fallback
        return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    /// <summary>
    /// Try to parse boolean from various representations
    /// </summary>
    private bool TryParseBoolean(string value, out bool result)
    {
        var normalized = value.Trim().ToLowerInvariant();
        
        if (normalized == "true" || normalized == "1" || normalized == "yes" || normalized == "evet" || normalized == "e")
        {
            result = true;
            return true;
        }

        if (normalized == "false" || normalized == "0" || normalized == "no" || normalized == "hayır" || normalized == "h")
        {
            result = false;
            return true;
        }

        result = false;
        return false;
    }

    /// <summary>
    /// 6.2.1: Validate Excel schema against database table definition
    /// Returns validation result with errors if mismatch found
    /// </summary>
    private SchemaValidationResult ValidateSchema(
        Dictionary<int, string> excelHeaders,
        Dictionary<string, TableColumn> columnMap,
        DecisionTreeTable table)
    {
        var result = new SchemaValidationResult { IsValid = true };

        // Get all required columns from database (excluding optional ones)
        var requiredDbColumns = table.Columns
            .Where(c => c.IsRequired && c.StatusCode == EntityStatusCode.Active)
            .ToList();

        // Check if all required DB columns exist in Excel
        foreach (var dbColumn in requiredDbColumns)
        {
            bool found = false;
            
            // Check if column name or excel header name exists in Excel
            foreach (var excelHeader in excelHeaders.Values)
            {
                if (excelHeader.Equals(dbColumn.ColumnName, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(dbColumn.ExcelHeaderName) && 
                     excelHeader.Equals(dbColumn.ExcelHeaderName, StringComparison.OrdinalIgnoreCase)))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                result.IsValid = false;
                result.Errors.Add($"Required column '{dbColumn.ColumnName}' not found in Excel file");
            }
        }

        // Warning: Check for unmatched Excel columns (columns in Excel that don't exist in DB)
        foreach (var excelHeader in excelHeaders.Values)
        {
            if (!columnMap.ContainsKey(excelHeader))
            {
                result.Warnings.Add($"Excel column '{excelHeader}' does not match any database column (will be ignored)");
            }
        }

        return result;
    }

    /// <summary>
    /// Write data to Excel file
    /// Creates one worksheet per table
    /// First row = column headers
    /// Following rows = data
    /// </summary>
    public async Task<byte[]> WriteExcelAsync(
        Dictionary<string, TableDataResult> tableData,
        List<DecisionTreeTable> tables,
        CancellationToken ct = default)
    {
        using var memoryStream = new MemoryStream();
        using var package = new ExcelPackage(memoryStream);

        foreach (var table in tables)
        {
            if (!tableData.TryGetValue(table.TableName, out var data) || data.Rows.Count == 0)
            {
                continue; // Skip tables with no data
            }

            var worksheet = package.Workbook.Worksheets.Add(table.TableName);
            
            // Get active columns sorted by order
            var columns = table.Columns
                .Where(c => c.StatusCode == EntityStatusCode.Active)
                .OrderBy(c => c.OrderIndex)
                .ThenBy(c => c.Id)
                .ToList();

            // Write headers (row 1)
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var headerName = !string.IsNullOrWhiteSpace(column.ExcelHeaderName)
                    ? column.ExcelHeaderName
                    : column.ColumnName;
                
                worksheet.Cells[1, i + 1].Value = headerName;
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            }

            // Write data rows
            int rowIndex = 2;
            foreach (var row in data.Rows)
            {
                for (int i = 0; i < columns.Count; i++)
                {
                    var column = columns[i];
                    
                    if (row.TryGetValue(column.ColumnName, out var value) && value != null)
                    {
                        var cell = worksheet.Cells[rowIndex, i + 1];
                        
                        // Format based on data type
                        switch (column.DataType)
                        {
                            case ColumnDataType.Date when value is DateTime dateTime:
                                cell.Value = dateTime;
                                cell.Style.Numberformat.Format = column.Format ?? "dd/MM/yyyy";
                                break;
                            
                            case ColumnDataType.Decimal when value is decimal:
                                cell.Value = value;
                                cell.Style.Numberformat.Format = "#,##0.00";
                                break;
                            
                            case ColumnDataType.Int when value is int:
                                cell.Value = value;
                                cell.Style.Numberformat.Format = "#,##0";
                                break;
                            
                            default:
                                cell.Value = value.ToString();
                                break;
                        }
                    }
                }
                rowIndex++;
            }

            // Auto-fit columns
            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
        }

        // Save to memory stream and return as byte array
        package.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }

    /// <summary>
    /// Export table data as CSV format (universal compatibility)
    /// One CSV file = one table (or combined if multiple tables)
    /// </summary>
    public async Task<byte[]> WriteCsvAsync(
        Dictionary<string, TableDataResult> tableData,
        List<DecisionTreeTable> tables,
        CancellationToken ct = default)
    {
        using var stringWriter = new StringWriter();
        
        bool isFirstTable = true;

        foreach (var table in tables)
        {
            if (!tableData.TryGetValue(table.TableName, out var data) || data.Rows.Count == 0)
            {
                continue; // Skip tables with no data
            }

            // Add table separator if multiple tables
            if (!isFirstTable)
            {
                stringWriter.WriteLine();
                stringWriter.WriteLine($"--- {table.TableName} ---");
            }
            isFirstTable = false;

            // Get active columns sorted by order
            var columns = table.Columns
                .Where(c => c.StatusCode == EntityStatusCode.Active)
                .OrderBy(c => c.OrderIndex)
                .ThenBy(c => c.Id)
                .ToList();

            // Write header row
            var headerRow = string.Join(",", columns.Select(c => 
            {
                var headerName = !string.IsNullOrWhiteSpace(c.ExcelHeaderName) ? c.ExcelHeaderName : c.ColumnName;
                return EscapeCsvValue(headerName);
            }));
            stringWriter.WriteLine(headerRow);

            // Write data rows
            foreach (var row in data.Rows)
            {
                var csvRow = string.Join(",", columns.Select(c =>
                {
                    if (row.TryGetValue(c.ColumnName, out var value) && value != null)
                    {
                        var stringValue = value switch
                        {
                            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                            decimal d => d.ToString("F2", CultureInfo.InvariantCulture),
                            _ => value.ToString() ?? ""
                        };
                        return EscapeCsvValue(stringValue);
                    }
                    return "";
                }));
                stringWriter.WriteLine(csvRow);
            }
        }

        var csvContent = stringWriter.ToString();
        return await Task.FromResult(System.Text.Encoding.UTF8.GetBytes(csvContent));
    }

    /// <summary>
    /// Escape CSV values containing commas or quotes
    /// </summary>
    private static string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        
        return value;
    }

    /// <summary>
    /// Read CSV file and convert to structured data
    /// CSV format: First row = column headers, Following rows = data
    /// Supports both single table and multiple tables (separated by "--- TableName ---")
    /// </summary>
    public async Task<ExcelReadResult> ReadCsvAsync(
        Stream csvStream,
        List<DecisionTreeTable> tables,
        CancellationToken ct = default)
    {
        var result = new ExcelReadResult { Success = true };

        try
        {
            using var reader = new StreamReader(csvStream, System.Text.Encoding.UTF8, true);
            var content = await reader.ReadToEndAsync(ct);
            
            // Check if it's a multi-table CSV (contains table separators)
            if (content.Contains("---"))
            {
                // Parse multi-table CSV
                result = ReadMultiTableCsv(content, tables);
            }
            else
            {
                // Parse single table CSV - use first table that has data
                var tableData = ReadSingleTableCsv(content, tables.FirstOrDefault());
                
                if (tableData.Errors.Any())
                {
                    result.Success = false;
                    result.Errors.AddRange(tableData.Errors);
                }
                else
                {
                    result.Warnings.AddRange(tableData.Warnings);
                    if (tableData.Rows.Count > 0 && tables.Any())
                    {
                        result.TableData[tables.First().TableName] = tableData;
                    }
                }
            }

            if (result.TableData.Count == 0)
            {
                result.Warnings.Add("No data found in CSV file");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading CSV file");
            result.Success = false;
            result.Errors.Add($"Failed to read CSV file: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Parse multi-table CSV format (with table separators)
    /// </summary>
    private ExcelReadResult ReadMultiTableCsv(string content, List<DecisionTreeTable> tables)
    {
        var result = new ExcelReadResult { Success = true };
        
        // Split by table separators
        var sections = content.Split(new[] { "---" }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var section in sections)
        {
            var lines = section.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) continue;
            
            // First line might be table name
            var firstLine = lines[0].Trim();
            string? tableName = null;
            int dataStartIndex = 0;
            
            // Check if first line is a table name (contains table name pattern)
            if (firstLine.Contains(":"))
            {
                tableName = firstLine.Replace(":", "").Trim();
                dataStartIndex = 1;
            }
            else
            {
                // Try to find matching table
                tableName = tables.FirstOrDefault(t => 
                    content.Contains(t.TableName, StringComparison.OrdinalIgnoreCase))?.TableName;
            }
            
            if (string.IsNullOrEmpty(tableName)) continue;
            
            var table = tables.FirstOrDefault(t => t.TableName.Equals(tableName, StringComparison.OrdinalIgnoreCase));
            if (table == null)
            {
                result.Warnings.Add($"Table '{tableName}' not found in database schema");
                continue;
            }
            
            var sectionContent = string.Join("\n", lines.Skip(dataStartIndex));
            var tableData = ReadSingleTableCsv(sectionContent, table);
            
            if (tableData.Errors.Any())
            {
                result.Errors.AddRange(tableData.Errors.Select(e => $"[{tableName}] {e}"));
            }
            
            result.Warnings.AddRange(tableData.Warnings.Select(w => $"[{tableName}] {w}"));
            
            if (tableData.Rows.Count > 0)
            {
                result.TableData[table.TableName] = tableData;
            }
        }
        
        return result;
    }

    /// <summary>
    /// Read single table CSV content
    /// </summary>
    private TableDataResult ReadSingleTableCsv(string content, DecisionTreeTable? table)
    {
        var result = new TableDataResult();
        
        if (table == null)
        {
            result.Errors.Add("No table specified");
            return result;
        }
        
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length < 2)
        {
            result.Warnings.Add("CSV file has no data rows");
            return result;
        }
        
        // Parse header row
        var headers = ParseCsvLine(lines[0]);
        
        // Create column mapping
        var columnMap = new Dictionary<string, TableColumn>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var column in table.Columns.Where(c => c.StatusCode == EntityStatusCode.Active))
        {
            columnMap[column.ColumnName] = column;
            
            if (!string.IsNullOrWhiteSpace(column.ExcelHeaderName))
            {
                columnMap[column.ExcelHeaderName] = column;
            }
        }
        
        // Validate schema
        var schemaValidation = ValidateCsvSchema(headers, columnMap, table);
        if (!schemaValidation.IsValid)
        {
            result.Errors.AddRange(schemaValidation.Errors);
            return result;
        }
        
        result.Warnings.AddRange(schemaValidation.Warnings);
        
        // Get unique identifier column
        var uniqueIdentifierColumn = table.Columns
            .FirstOrDefault(c => c.IsUniqueIdentifier && c.StatusCode == EntityStatusCode.Active);
        
        if (uniqueIdentifierColumn != null)
        {
            result.UniqueIdentifierColumnName = uniqueIdentifierColumn.ColumnName;
        }
        
        // Read data rows
        for (int rowIndex = 1; rowIndex < lines.Length; rowIndex++)
        {
            var values = ParseCsvLine(lines[rowIndex]);
            
            if (values.Length == 0 || values.All(string.IsNullOrWhiteSpace))
            {
                continue; // Skip empty rows
            }
            
            var rowData = new Dictionary<string, object?>();
            var rowErrors = new List<string>();
            bool hasAnyData = false;
            
            for (int colIndex = 0; colIndex < headers.Length && colIndex < values.Length; colIndex++)
            {
                var headerName = headers[colIndex];
                
                if (!columnMap.TryGetValue(headerName, out var columnDef))
                {
                    continue; // Skip unknown columns
                }
                
                var cellValue = values[colIndex]?.Trim();
                
                if (string.IsNullOrWhiteSpace(cellValue))
                {
                    if (columnDef.IsRequired)
                    {
                        rowErrors.Add($"Row {rowIndex + 1}: Required column '{columnDef.ColumnName}' is empty");
                    }
                    rowData[columnDef.ColumnName] = null;
                    continue;
                }
                
                hasAnyData = true;
                
                // Convert value based on data type
                var (convertedValue, error) = ConvertCellValue(cellValue, columnDef, rowIndex + 1);
                
                if (error != null)
                {
                    rowErrors.Add(error);
                }
                
                rowData[columnDef.ColumnName] = convertedValue;
            }
            
            if (!hasAnyData)
            {
                continue;
            }
            
            if (rowErrors.Any())
            {
                result.Errors.AddRange(rowErrors);
            }
            
            result.Rows.Add(rowData);
        }
        
        return result;
    }

    /// <summary>
    /// Parse a CSV line handling quoted values
    /// </summary>
    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var currentValue = new System.Text.StringBuilder();
        bool inQuotes = false;
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Escaped quote
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }
        
        result.Add(currentValue.ToString());
        return result.ToArray();
    }

    /// <summary>
    /// Validate CSV schema against database table definition
    /// </summary>
    private SchemaValidationResult ValidateCsvSchema(
        string[] csvHeaders,
        Dictionary<string, TableColumn> columnMap,
        DecisionTreeTable table)
    {
        var result = new SchemaValidationResult { IsValid = true };
        
        // Get headers as a list for comparison
        var headerList = csvHeaders.ToList();
        
        // Check if all required DB columns exist in CSV
        var requiredDbColumns = table.Columns
            .Where(c => c.IsRequired && c.StatusCode == EntityStatusCode.Active)
            .ToList();
        
        foreach (var dbColumn in requiredDbColumns)
        {
            bool found = false;
            
            foreach (var csvHeader in headerList)
            {
                if (csvHeader.Equals(dbColumn.ColumnName, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(dbColumn.ExcelHeaderName) && 
                     csvHeader.Equals(dbColumn.ExcelHeaderName, StringComparison.OrdinalIgnoreCase)))
                {
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                result.IsValid = false;
                result.Errors.Add($"Required column '{dbColumn.ColumnName}' not found in CSV file");
            }
        }
        
        // Warning: Check for unmatched CSV columns
        foreach (var csvHeader in headerList)
        {
            if (!columnMap.ContainsKey(csvHeader))
            {
                result.Warnings.Add($"CSV column '{csvHeader}' does not match any database column (will be ignored)");
            }
        }
        
        return result;
    }
}

/// <summary>
/// Result of Excel read operation
/// </summary>
public class ExcelReadResult
{
    public bool Success { get; set; }
    public Dictionary<string, TableDataResult> TableData { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Data from a single table/worksheet
/// 6.2.2: Stores unique identifier column name for row matching
/// </summary>
public class TableDataResult
{
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    /// <summary>
    /// Column name marked as unique identifier for row matching
    /// Used during import to match Excel rows to existing DB records
    /// </summary>
    public string? UniqueIdentifierColumnName { get; set; }
}

/// <summary>
/// Result of schema validation
/// </summary>
public class SchemaValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
