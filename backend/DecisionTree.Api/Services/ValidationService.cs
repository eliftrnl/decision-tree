using DecisionTree.Api.Contracts.DataEntry;
using DecisionTree.Api.Data;
using DecisionTree.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using EntityStatusCode = DecisionTree.Api.Entities.StatusCode;

namespace DecisionTree.Api.Services;

/// <summary>
/// Service for validating data against column metadata
/// Catches errors without throwing (reports them instead)
/// Supports all data types: String, Int, Decimal, Date, Boolean
/// </summary>
public class ValidationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ValidationService> _logger;

    public ValidationService(AppDbContext db, ILogger<ValidationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Validate a single row against column definitions
    /// Returns validation result with errors (never throws)
    /// </summary>
    public async Task<ValidationResult> ValidateRowAsync(
        int tableId,
        Dictionary<string, object?> rowData,
        int rowIndex,
        CancellationToken ct = default)
    {
        var result = new ValidationResult { IsValid = true };

        // Load table with columns
        var table = await _db.DecisionTreeTables
            .Include(x => x.Columns)
            .FirstOrDefaultAsync(x => x.Id == tableId, ct);

        if (table is null)
        {
            result.IsValid = false;
            result.Errors.Add($"Table with id {tableId} not found");
            return result;
        }

        var activeColumns = table.Columns
            .Where(c => c.StatusCode == EntityStatusCode.Active)
            .ToList();

        // Check each column
        foreach (var column in activeColumns)
        {
            var hasValue = rowData.TryGetValue(column.ColumnName, out var value);

            // Check required fields
            if (column.IsRequired && (!hasValue || value is null))
            {
                result.IsValid = false;
                result.Errors.Add(
                    $"Row {rowIndex}, Column '{column.ColumnName}': Required field is empty");
                continue;
            }

            // Skip validation if no value and not required
            if (!hasValue || value is null)
                continue;

            // Validate data type and format
            var (isValid, error) = ValidateValue(value, column, rowIndex);
            if (!isValid)
            {
                result.IsValid = false;
                result.Errors.Add(error!);
            }
        }

        // Check for unknown columns (columns in data but not in metadata)
        foreach (var key in rowData.Keys)
        {
            if (!activeColumns.Any(c => c.ColumnName == key))
            {
                result.Warnings.Add($"Row {rowIndex}: Unknown column '{key}' (not in metadata)");
            }
        }

        return result;
    }

    /// <summary>
    /// Validate a single value against column definition
    /// </summary>
    private (bool isValid, string? error) ValidateValue(
        object? value,
        TableColumn column,
        int rowIndex)
    {
        if (value is null)
            return (true, null);

        try
        {
            return column.DataType switch
            {
                ColumnDataType.String => ValidateString(value, column, rowIndex),
                ColumnDataType.Int => ValidateInt(value, column, rowIndex),
                ColumnDataType.Decimal => ValidateDecimal(value, column, rowIndex),
                ColumnDataType.Date => ValidateDate(value, column, rowIndex),
                ColumnDataType.Boolean => ValidateBoolean(value, column, rowIndex),
                _ => (true, null)
            };
        }
        catch (Exception ex)
        {
            return (false, 
                $"Row {rowIndex}, Column '{column.ColumnName}': Unexpected validation error - {ex.Message}");
        }
    }

    private (bool isValid, string? error) ValidateString(object value, TableColumn column, int rowIndex)
    {
        var str = value?.ToString() ?? string.Empty;

        // Check max length
        if (column.MaxLength.HasValue && str.Length > column.MaxLength.Value)
        {
            return (false,
                $"Row {rowIndex}, Column '{column.ColumnName}': Length {str.Length} exceeds max {column.MaxLength}");
        }

        return (true, null);
    }

    private (bool isValid, string? error) ValidateInt(object value, TableColumn column, int rowIndex)
    {
        var str = value?.ToString() ?? string.Empty;

        if (!int.TryParse(str, out var intVal))
        {
            return (false,
                $"Row {rowIndex}, Column '{column.ColumnName}': '{str}' is not a valid integer");
        }

        return (true, null);
    }

    private (bool isValid, string? error) ValidateDecimal(object value, TableColumn column, int rowIndex)
    {
        var str = value?.ToString() ?? string.Empty;

        if (!decimal.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var decVal))
        {
            return (false,
                $"Row {rowIndex}, Column '{column.ColumnName}': '{str}' is not a valid decimal");
        }

        // Check precision/scale if specified
        if (column.Precision.HasValue && column.Scale.HasValue)
        {
            var absVal = Math.Abs(decVal);
            var intPartLen = absVal >= 1
                ? (int)Math.Floor(Math.Log10((double)absVal)) + 1
                : 0;

            if (intPartLen > (column.Precision.Value - column.Scale.Value))
            {
                return (false,
                    $"Row {rowIndex}, Column '{column.ColumnName}': Value exceeds precision({column.Precision},{column.Scale})");
            }
        }

        return (true, null);
    }

    private (bool isValid, string? error) ValidateDate(object value, TableColumn column, int rowIndex)
    {
        var str = value?.ToString() ?? string.Empty;

        // Try with format hint first
        if (!string.IsNullOrWhiteSpace(column.Format))
        {
            if (DateTime.TryParseExact(str, column.Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return (true, null);
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
            if (DateTime.TryParseExact(str, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return (true, null);
            }
        }

        // General parse as fallback
        if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            return (true, null);
        }

        return (false,
            $"Row {rowIndex}, Column '{column.ColumnName}': '{str}' is not a valid date (expected format: {column.Format ?? "dd/MM/yyyy"})");
    }

    private (bool isValid, string? error) ValidateBoolean(object value, TableColumn column, int rowIndex)
    {
        var str = value?.ToString()?.Trim().ToLowerInvariant() ?? string.Empty;

        var trueValues = new[] { "true", "1", "yes", "evet", "e", "t", "y" };
        var falseValues = new[] { "false", "0", "no", "hayır", "h", "f", "n" };

        if (trueValues.Contains(str) || falseValues.Contains(str))
            return (true, null);

        return (false,
            $"Row {rowIndex}, Column '{column.ColumnName}': '{value}' is not a valid boolean (expected: true/false, yes/no, evet/hayır)");
    }

    /// <summary>
    /// Validate all rows for a table at once
    /// </summary>
    public async Task<ValidationResult> ValidateAllRowsAsync(
        int tableId,
        List<Dictionary<string, object?>> rows,
        CancellationToken ct = default)
    {
        var result = new ValidationResult { IsValid = true };

        for (int i = 0; i < rows.Count; i++)
        {
            var rowResult = await ValidateRowAsync(tableId, rows[i], i + 2, ct); // +2 because row 1 is header
            
            if (!rowResult.IsValid)
            {
                result.IsValid = false;
                result.Errors.AddRange(rowResult.Errors);
            }

            result.Warnings.AddRange(rowResult.Warnings);
        }

        return result;
    }
}

/// <summary>
/// Validation result containing errors and warnings
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public int ErrorCount => Errors.Count;
    public int WarningCount => Warnings.Count;
}
