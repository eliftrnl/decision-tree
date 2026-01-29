using System;
using System.Collections.Generic;

namespace DecisionTree.Api.Entities;

public class DecisionTreeTable
{
    public int Id { get; set; }

    public int DecisionTreeId { get; set; }
    public DecisionTree DecisionTree { get; set; } = null!;

    public string TableCode { get; set; } = null!;
    public string? TableName { get; set; }
    public TableDirection Direction { get; set; } = TableDirection.Input;

    public StatusCode StatusCode { get; set; } = StatusCode.Active;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public List<TableColumn> Columns { get; set; } = new();
}
