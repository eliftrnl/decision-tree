namespace DecisionTree.Api.Contracts.DecisionTrees;

public sealed record DecisionTreeUpdateRequest(
    string Name,
    int StatusCode
);
