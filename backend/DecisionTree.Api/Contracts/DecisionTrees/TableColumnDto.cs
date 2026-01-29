namespace DecisionTree.Api.Contracts.DecisionTrees;

public record TableColumnDto(
    int Id,
    int TableId,
    string ColumnName,
    string? ExcelHeaderName,
    string? Description,
    string DataType, // "String", "Int", "Decimal", "Date", "Boolean"
    bool IsRequired,
    string StatusCode, // "Active" or "Passive"
    int OrderIndex,
    string? Format,
    int? MaxLength,
    int? Precision,
    int? Scale,
    DateTime? ValidFrom,
    DateTime? ValidTo
);

public record TableColumnCreateRequest(
    int TableId,
    string ColumnName,
    string? ExcelHeaderName,
    string? Description,
    int DataType,
    bool IsRequired,
    int StatusCode,
    int OrderIndex,
    string? Format,
    int? MaxLength,
    int? Precision,
    int? Scale,
    DateTime? ValidFrom,
    DateTime? ValidTo
);

public record TableColumnUpdateRequest(
    string ColumnName,
    string? ExcelHeaderName,
    string? Description,
    int DataType,
    bool IsRequired,
    int StatusCode,
    int OrderIndex,
    string? Format,
    int? MaxLength,
    int? Precision,
    int? Scale,
    DateTime? ValidFrom,
    DateTime? ValidTo
);

public record ReorderColumnsRequest(
    List<int> ColumnIds
);
