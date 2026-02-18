namespace DecisionTree.Api.Contracts.DataEntry;

/// <summary>
/// Request to validate a single row against column metadata
/// </summary>
public record ValidateRowRequest(
    int TableId,
    Dictionary<string, object?> RowData
);

/// <summary>
/// Response from row validation
/// </summary>
public record ValidateRowResponse(
    bool IsValid,
    List<string> Errors,
    List<string> Warnings
);

/// <summary>
/// Request to reorder columns
/// </summary>
public record ReorderColumnsRequest(
    List<ColumnOrderItem> Columns
);

/// <summary>
/// Column order item
/// </summary>
public record ColumnOrderItem(
    int ColumnId,
    int NewOrderIndex
);
