namespace DecisionTree.Api.Entities
{
    public enum StatusCode
    {
        Active = 1,
        Passive = 2
    }

    public enum TableDirection
    {
        Input = 1,
        Output = 2
    }

    public enum ColumnDataType
    {
        String = 1,
        Int = 2,
        Decimal = 3,
        Date = 4,
        Boolean = 5
    }
}
