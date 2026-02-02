namespace DecisionTree.Api.Contracts.DataEntry;

/// <summary>
/// Request for importing Excel file
/// Accepts file as multipart/form-data
/// </summary>
public class ExcelImportRequest
{
    /// <summary>
    /// Excel file to import
    /// </summary>
    public IFormFile File { get; set; } = null!;

    /// <summary>
    /// If true, replace all existing data for this decision tree
    /// If false, merge with existing data
    /// </summary>
    public bool ReplaceExistingData { get; set; } = false;

    /// <summary>
    /// If true, continue even if some rows have errors
    /// If false, abort on first error
    /// </summary>
    public bool ContinueOnError { get; set; } = true;
}

/// <summary>
/// Response when exporting data to Excel
/// Contains base64-encoded file content and metadata
/// </summary>
public record ExcelExportResponse(
    string FileName,
    string FileContent, // base64 encoded
    string ContentType,
    long FileSizeBytes,
    DateTime ExportedAtUtc,
    int TotalRowsExported
);

/// <summary>
/// Response for Excel import with validation results
/// </summary>
public record ExcelImportResponse(
    bool Success,
    int TotalRowsProcessed,
    int TotalRowsImported,
    int TotalErrors,
    List<ExcelImportError> Errors,
    List<ExcelImportWarning> Warnings
);

/// <summary>
/// Error during Excel import
/// Points to specific row and column for debugging
/// </summary>
public record ExcelImportError(
    string TableName,
    int RowNumber,
    string ColumnName,
    string ErrorMessage
);

/// <summary>
/// Warning during Excel import
/// Non-fatal issues that should be reviewed
/// </summary>
public record ExcelImportWarning(
    string TableName,
    int RowNumber,
    string? ColumnName,
    string WarningMessage
);
