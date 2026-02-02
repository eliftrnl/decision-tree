namespace DecisionTree.Api.Contracts.DataEntry;

/// <summary>
/// Complete JSON export response with metadata and data
/// Used for exporting decision tree data as JSON
/// </summary>
public record JsonExportResponse(
    JsonMetadata Metadata,
    List<JsonTableOutput> Tables
);

/// <summary>
/// Metadata section of JSON export
/// Tracks which decision tree and schema version
/// </summary>
public record JsonMetadata(
    int DecisionTreeId,
    string DecisionTreeCode,
    string DecisionTreeName,
    int SchemaVersion,
    DateTime ExportedAtUtc
);

/// <summary>
/// Table section in JSON export
/// Each table can have metadata (columns) and data (rows)
/// Tables with no data are excluded from export
/// </summary>
public record JsonTableOutput(
    int TableId,
    string TableName,
    string Direction, // "Input" or "Output"
    List<JsonColumnMetadata> Columns,
    List<Dictionary<string, object?>> Rows
);

/// <summary>
/// Column metadata in JSON export
/// Describes the structure and validation rules for columns
/// </summary>
public record JsonColumnMetadata(
    int ColumnId,
    string ColumnName,
    string DataType, // "String", "Int", "Decimal", "Date", "Boolean"
    bool IsRequired,
    string? Format, // e.g., "dd/MM/yyyy"
    int OrderIndex
);
