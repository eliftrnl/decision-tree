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
        using var package = new ExcelPackage();

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

        return await Task.FromResult(package.GetAsByteArray());
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
/// </summary>
public class TableDataResult
{
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
