namespace DecisionTree.Api.Contracts.DataEntry;

/// <summary>
/// DTO for DecisionTreeData response
/// </summary>
public record DataRowDto(
    int Id,
    int TableId,
    string RowDataJson,
    string? RowCode,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);

/// <summary>
/// Request for creating a new data row
/// </summary>
public record DataRowCreateRequest(
    int TableId,
    string RowDataJson,
    string? RowCode = null
);

/// <summary>
/// Request for updating an existing data row
/// </summary>
public record DataRowUpdateRequest(
    string RowDataJson,
    string? RowCode = null
);

/// <summary>
/// Request for generating JSON output (metadata + data combined)
/// </summary>
public record GenerateJsonRequest(
    int DecisionTreeId,
    bool IncludeInactiveTables = false,
    bool IncludeInactiveColumns = false
);

/// <summary>
/// Request for parsing JSON input and storing data
/// </summary>
public record ParseJsonRequest(
    int DecisionTreeId,
    string JsonContent,
    bool ReplaceExistingData = false
);

/// <summary>
/// Response for JSON generation with metadata + data
/// </summary>
public record JsonOutputResponse(
    string DecisionTreeCode,
    string DecisionTreeName,
    int SchemaVersion,
    DateTime GeneratedAtUtc,
    List<TableJsonOutput> Tables
);

/// <summary>
/// Table section in JSON output
/// </summary>
public record TableJsonOutput(
    string TableCode,
    string TableName,
    string Direction,
    List<ColumnMetadata> Columns,
    List<Dictionary<string, object?>> Rows
);

/// <summary>
/// Column metadata in JSON output
/// </summary>
public record ColumnMetadata(
    string ColumnCode,
    string ColumnName,
    string DataType,
    bool IsRequired,
    int OrderIndex
);
