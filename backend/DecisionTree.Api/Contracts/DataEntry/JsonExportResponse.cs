namespace DecisionTree.Api.Contracts.DataEntry;

/// <summary>
/// Complete JSON export response with metadata, inputs and outputs
/// Used for exporting decision tree data as JSON
/// </summary>
public record JsonExportResponse(
    JsonExportMetadata Metadata,
    List<JsonTableWrapper> Inputs,
    List<JsonTableWrapper> Outputs
);

/// <summary>
/// Metadata for JSON export - decision tree information
/// </summary>
public record JsonExportMetadata(
    int DecisionTreeId,
    string DecisionTreeCode,
    string DecisionTreeName,
    int SchemaVersion,
    DateTime ExportedAt
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
/// Metadata describes column definitions as a list of dictionaries (one per column)
/// Data contains arrays of row values
/// </summary>
public record JsonTableValue(
    List<Dictionary<string, string>> metadata,
    List<List<object?>> data
);

/// <summary>
/// JSON export response with only inputs (no metadata or outputs)
/// Used for simplified JSON export format: { "inputs": [...] }
/// </summary>
public record JsonInputsOnlyResponse(
    List<JsonTableWrapper> Inputs
);
