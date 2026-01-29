using System;
using System.Text.Json.Serialization;

namespace DecisionTree.Api.Entities;

public sealed class TableColumn
{
    public int Id { get; set; }

    public int TableId { get; set; }

    [JsonIgnore]
    public DecisionTreeTable Table { get; set; } = null!;

    /// <summary>
    /// Column name - used for JSON keys, DB mapping, and display
    /// </summary>
    public string ColumnName { get; set; } = null!;

    /// <summary>
    /// Excel header name (if different from ColumnCode)
    /// </summary>
    public string? ExcelHeaderName { get; set; }

    /// <summary>
    /// Description for documentation
    /// </summary>
    public string? Description { get; set; }

    public ColumnDataType DataType { get; set; }

    public bool IsRequired { get; set; } = false;

    public StatusCode StatusCode { get; set; } = StatusCode.Active;

    /// <summary>
    /// UI order index (not used for data mapping)
    /// </summary>
    public int OrderIndex { get; set; } = 0;

    public string? Format { get; set; }

    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }

    /// <summary>

    /// Validity period for temporal columns
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }
}
