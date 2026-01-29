namespace DecisionTree.Api.Contracts.DecisionTrees;

public record DecisionTreeTableDto(
    int Id,
    int DecisionTreeId,
    string TableCode,
    string? TableName,
    string Direction, // "Input" or "Output"
    string StatusCode // "Active" or "Passive"
);

public record DecisionTreeTableCreateRequest(
    int DecisionTreeId,
    string TableCode,
    string? TableName,
    string Direction,
    int StatusCode
);

public record DecisionTreeTableUpdateRequest(
    string TableCode,
    string? TableName,
    string Direction,
    int StatusCode
);
