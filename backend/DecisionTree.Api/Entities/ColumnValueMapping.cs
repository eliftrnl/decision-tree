namespace DecisionTree.Api.Entities;

/// <summary>
/// Tracks column position changes (for handling when Excel columns are reordered)
/// </summary>
public class ColumnValueMapping
{
    public int Id { get; set; }
    
    public int TableColumnId { get; set; }
    public TableColumn TableColumn { get; set; } = null!;
    
    /// <summary>
    /// Previous column position in Excel/data source
    /// </summary>
    public int OldPosition { get; set; }
    
    /// <summary>
    /// New column position in DB schema
    /// </summary>
    public int NewPosition { get; set; }
    
    public DateTime ChangedAtUtc { get; set; } = DateTime.UtcNow;
}
