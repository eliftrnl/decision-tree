namespace DecisionTree.Api.Contracts.DataEntry;

/// <summary>
/// Complete JSON export response with metadata and tables
/// Used for exporting decision tree data as JSON
/// </summary>
public record JsonExportResponse(
    JsonMetadata Metadata,
    List<JsonTableWrapper> Tables
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
/// Table wrapper for JSON export
/// Each table has a name and value containing metadata and data arrays
/// </summary>
public record JsonTableWrapper(
    string name,
    JsonTableValue value
);

/// <summary>
/// Table value containing metadata and data arrays
/// Metadata describes column types and names
/// Data contains arrays of row values
/// </summary>
public record JsonTableValue(
    List<Dictionary<string, string>> metadata,
    List<List<object?>> data
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
