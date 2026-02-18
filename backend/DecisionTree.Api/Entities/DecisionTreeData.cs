namespace DecisionTree.Api.Entities;

/// <summary>
/// Stores data rows for a specific table in JSON format
/// </summary>
public class DecisionTreeData
{
    public int Id { get; set; }
    
    /// <summary>
    /// Foreign key to DecisionTree
    /// </summary>
    public int DecisionTreeId { get; set; }
    
    /// <summary>
    /// Foreign key to DecisionTreeTable
    /// </summary>
    public int TableId { get; set; }
    
    /// <summary>
    /// Row index/sequence number
    /// </summary>
    public int RowIndex { get; set; }
    
    /// <summary>
    /// Row data stored as JSON (key-value pairs where key is ColumnName)
    /// Example: {"MusteriNo": "1001", "KimlikNo": "12345678901"}
    /// </summary>
    public string RowDataJson { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional row code/identifier for Excel row matching
    /// Used with IsUniqueIdentifier column during import/export
    /// Example: "TOEI_001" or unique identifier value from matching column
    /// </summary>
    public string? RowCode { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    // Navigation
    public DecisionTree DecisionTree { get; set; } = null!;
    public DecisionTreeTable Table { get; set; } = null!;
}
