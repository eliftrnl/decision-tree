using DecisionTree.Api.Contracts.DataEntry;
using DecisionTree.Api.Data;
using DecisionTree.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using EntityStatusCode = DecisionTree.Api.Entities.StatusCode;

namespace DecisionTree.Api.Services;

/// <summary>
/// Service for building JSON exports from decision tree data
/// Handles metadata collection and data serialization
/// </summary>
public class JsonBuilderService
{
    private readonly AppDbContext _db;
    private readonly ILogger<JsonBuilderService> _logger;

    public JsonBuilderService(AppDbContext db, ILogger<JsonBuilderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Build complete JSON export with metadata and data
    /// Only includes tables that have at least one data row
    /// Respects column order and active/passive status
    /// </summary>
    public async Task<JsonExportResponse> BuildJsonExportAsync(
        int decisionTreeId,
        bool includeInactiveTables = false,
        bool includeInactiveColumns = false,
        CancellationToken ct = default)
    {
        // Load decision tree with all related data
        var dt = await _db.DecisionTrees
            .Include(x => x.Tables)
            .ThenInclude(x => x.Columns)
            .FirstOrDefaultAsync(x => x.Id == decisionTreeId, ct);

        if (dt is null)
            throw new InvalidOperationException($"Decision tree with id {decisionTreeId} not found");

        // Build metadata
        var metadata = new JsonMetadata(
            DecisionTreeId: dt.Id,
            DecisionTreeCode: dt.Code,
            DecisionTreeName: dt.Name,
            SchemaVersion: dt.SchemaVersion,
            ExportedAtUtc: DateTime.UtcNow
        );

        // Build tables
        var tables = new List<JsonTableOutput>();

        foreach (var table in dt.Tables)
        {
            // Skip inactive tables unless requested
            if (table.StatusCode == EntityStatusCode.Passive && !includeInactiveTables)
                continue;

            // Get data rows for this table
            var dataRows = await _db.DecisionTreeData
                .Where(x => x.TableId == table.Id)
                .OrderBy(x => x.RowIndex)
                .ToListAsync(ct);

            // Skip tables with no data
            if (dataRows.Count == 0)
            {
                _logger.LogDebug($"Skipping table '{table.TableName}' - no data rows");
                continue;
            }

            // Get active columns
            var columns = table.Columns
                .Where(c => c.StatusCode == EntityStatusCode.Active || includeInactiveColumns)
                .OrderBy(c => c.OrderIndex)
                .ThenBy(c => c.Id)
                .ToList();

            // Build column metadata
            var columnMetadata = columns.Select(c => new JsonColumnMetadata(
                ColumnId: c.Id,
                ColumnName: c.ColumnName,
                DataType: c.DataType.ToString(),
                IsRequired: c.IsRequired,
                Format: c.Format,
                OrderIndex: c.OrderIndex
            )).ToList();

            // Parse row data from JSON strings
            var rows = new List<Dictionary<string, object?>>();
            foreach (var dataRow in dataRows)
            {
                try
                {
                    var rowDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(dataRow.RowDataJson)
                        ?? new Dictionary<string, object?>();
                    rows.Add(rowDict);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, $"Error parsing row data for table {table.TableName}, row {dataRow.RowIndex}");
                    throw;
                }
            }

            // Build table output
            var tableOutput = new JsonTableOutput(
                TableId: table.Id,
                TableName: table.TableName,
                Direction: table.Direction.ToString(),
                Columns: columnMetadata,
                Rows: rows
            );

            tables.Add(tableOutput);
        }

        return new JsonExportResponse(metadata, tables);
    }

    /// <summary>
    /// Serialize JSON export to pretty-printed JSON string
    /// </summary>
    public string SerializeToJson(JsonExportResponse export)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = false
        };

        return JsonSerializer.Serialize(export, options);
    }

    /// <summary>
    /// Convert JSON export to formatted JSON string
    /// </summary>
    public async Task<string> BuildAndSerializeJsonAsync(
        int decisionTreeId,
        bool includeInactiveTables = false,
        bool includeInactiveColumns = false,
        CancellationToken ct = default)
    {
        var export = await BuildJsonExportAsync(decisionTreeId, includeInactiveTables, includeInactiveColumns, ct);
        return SerializeToJson(export);
    }
}
