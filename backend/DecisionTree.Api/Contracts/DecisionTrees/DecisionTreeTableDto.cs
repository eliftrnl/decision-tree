using System.Text.Json;

namespace DecisionTree.Api.Contracts.DecisionTrees;

public record DecisionTreeTableDto(
    int Id,
    int DecisionTreeId,
    string TableName,
    string Direction, // "Input" or "Output"
    string StatusCode // "Active" or "Passive"
);

public record DecisionTreeTableCreateRequest(
    int DecisionTreeId,
    string TableName,
    JsonElement Direction,
    int StatusCode
);

public record DecisionTreeTableUpdateRequest(
    string TableName,
    JsonElement Direction,
    int StatusCode
);
