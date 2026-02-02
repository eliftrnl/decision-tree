namespace DecisionTree.Api.Entities;

public enum ValidationErrorType
{
    REQUIRED = 1,
    TYPE_MISMATCH = 2,
    FORMAT = 3,
    RANGE = 4,
    LENGTH = 5,
    DUPLICATE = 6,
    UNKNOWN_COLUMN = 7
}

/// <summary>
/// Logs validation errors when importing data or importing from Excel
/// </summary>
public class ValidationLog
{
    public int Id { get; set; }
    
    public int DecisionTreeId { get; set; }
    public DecisionTree DecisionTree { get; set; } = null!;
    
    public int? TableId { get; set; }
    public DecisionTreeTable? Table { get; set; }
    
    public int? RowIndex { get; set; }
    
    public string ColumnName { get; set; } = string.Empty;
    
    public string? Value { get; set; }
    
    public ValidationErrorType ErrorType { get; set; }
    
    public string ErrorMessage { get; set; } = string.Empty;
    
    public DateTime LoggedAtUtc { get; set; } = DateTime.UtcNow;
}
