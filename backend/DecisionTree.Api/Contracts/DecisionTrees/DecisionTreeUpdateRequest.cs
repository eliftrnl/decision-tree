namespace DecisionTree.Api.Contracts.DecisionTrees;
//karar ağacını güncellemek isteyince, Frontend'den Backend'e hangi format gelmesi gerektiğini belirtir.


public sealed record DecisionTreeUpdateRequest(
    string Name,
    int StatusCode
);
