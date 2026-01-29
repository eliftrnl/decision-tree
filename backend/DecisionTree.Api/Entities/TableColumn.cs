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
    /// Technical column name (e.g., MusteriNo) - used for JSON keys and DB mapping
    /// </summary>
    public string ColumnCode { get; set; } = null!;

    /// <summary>
    /// Display name shown in UI
    /// </summary>
    public string? ColumnName { get; set; }

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
    /// Optional DB-specific column type (e.g., "varchar(50)", "decimal(10,2)")
    /// </summary>
    public string? ColumnType { get; set; }

    /// <summary>
    /// Validity period for temporal columns
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }
}
