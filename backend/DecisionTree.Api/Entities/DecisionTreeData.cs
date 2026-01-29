namespace DecisionTree.Api.Entities;

/// <summary>
/// Stores data rows for a specific table in JSON format
/// </summary>
public class DecisionTreeData
{
    public int Id { get; set; }
    
    /// <summary>
    /// Foreign key to DecisionTreeTable
    /// </summary>
    public int TableId { get; set; }
    
    /// <summary>
    /// Row data stored as JSON (key-value pairs where key is ColumnCode)
    /// Example: {"CustomerName": "John Doe", "Age": 30, "City": "Istanbul"}
    /// </summary>
    public string RowDataJson { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional row identifier/code for reference
    /// </summary>
    public string? RowCode { get; set; }
    
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    // Navigation
    public DecisionTreeTable Table { get; set; } = null!;
}
