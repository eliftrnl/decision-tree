namespace DecisionTree.Api.Contracts.DecisionTrees;

public sealed record DecisionTreeListItemDto(
    int Id,
    string Code,
    string Name,
    int StatusCode,
    DateTime? LastOperationDateUtc
);
