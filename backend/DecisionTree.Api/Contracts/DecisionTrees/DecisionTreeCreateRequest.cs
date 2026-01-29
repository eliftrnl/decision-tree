namespace DecisionTree.Api.Contracts.DecisionTrees;

public sealed record DecisionTreeCreateRequest(
    string Code,
    string Name
);
