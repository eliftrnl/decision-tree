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

    public JsonBuilderService(AppDbContext db, ILogger<JsonBuilderService> logger)//veritabanından karar ağacını yükle
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Build complete JSON export with metadata, inputs and outputs
    /// Only includes tables that have at least one data row
    /// Respects column order and active/passive status
    /// Separates tables by Direction (Input/Output)
    /// </summary>
    public async Task<JsonExportResponse> BuildJsonExportAsync(
        int decisionTreeId,
        bool includeInactiveTables = false,
        bool includeInactiveColumns = false,
        CancellationToken ct = default)
    {
        // Veritabanından ID ile karar ağacını getir
        var dt = await _db.DecisionTrees
            .Include(x => x.Tables)
            .ThenInclude(x => x.Columns)
            .FirstOrDefaultAsync(x => x.Id == decisionTreeId, ct);

        if (dt is null)
            throw new InvalidOperationException($"Decision tree with id {decisionTreeId} not found");

        // Build inputs and outputs
        var inputs = new List<JsonTableWrapper>();
        var outputs = new List<JsonTableWrapper>();

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

            // Build metadata list as list of dictionaries { "ColumnName": "DataType" }
            var metadataList = new List<Dictionary<string, string>>();
            foreach (var column in columns)
            {
                metadataList.Add(new Dictionary<string, string>
                {
                    { column.ColumnName, column.DataType.ToString().ToLowerInvariant() }
                });
            }

            // Parse row data and convert to arrays
            var dataArrays = new List<List<object?>>();
            foreach (var dataRow in dataRows)
            {
                try
                {
                    var rowDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(dataRow.RowDataJson)
                        ?? new Dictionary<string, object?>();
                    
                    // Convert row to array of values in column order
                    var rowArray = new List<object?>();
                    foreach (var column in columns)
                    {
                        if (rowDict.TryGetValue(column.ColumnName, out var value))
                        {
                            // In the image, values are numbers or strings. 
                            // Try to maintain original type if possible. 
                            // The current code converts to string: rowArray.Add(value?.ToString() ?? null);
                            // Let's try to keep objects as they are.
                            rowArray.Add(value);
                        }
                        else
                        {
                            rowArray.Add(null);
                        }
                    }
                    
                    dataArrays.Add(rowArray);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, $"Error parsing row data for table {table.TableName}, row {dataRow.RowIndex}");
                    throw;
                }
            }

            // Build table wrapper with name and value
            var tableWrapper = new JsonTableWrapper(
                name: table.TableName,
                value: new JsonTableValue(
                    metadata: metadataList,
                    data: dataArrays
                )
            );

            // Separate tables by direction
            if (table.Direction == TableDirection.Input)
            {
                inputs.Add(tableWrapper);
            }
            else if (table.Direction == TableDirection.Output)
            {
                outputs.Add(tableWrapper);
            }
        }

        // Build metadata
        var metadata = new JsonExportMetadata(
            DecisionTreeId: dt.Id,
            DecisionTreeCode: dt.Code,
            DecisionTreeName: dt.Name,
            SchemaVersion: dt.SchemaVersion,
            ExportedAt: DateTime.UtcNow
        );

        return new JsonExportResponse(metadata, inputs, outputs);
    }

    /// <summary>
    /// Serialize JSON export response to pretty-printed JSON string
    /// </summary>
    public string SerializeToJson(JsonExportResponse response)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = false
        };

        return JsonSerializer.Serialize(response, options);
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

    /// <summary>
    /// Build JSON export with only inputs (no metadata or outputs)
    /// Returns { "inputs": [...] } format
    /// </summary>
    public async Task<JsonInputsOnlyResponse> BuildJsonInputsOnlyAsync(
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

        var inputs = new List<JsonTableWrapper>();

        foreach (var table in dt.Tables)
        {
            // Skip inactive tables unless requested
            if (table.StatusCode == EntityStatusCode.Passive && !includeInactiveTables)
                continue;

            // Skip output tables - only include input tables
            if (table.Direction != TableDirection.Input)
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

            // Build metadata list as list of dictionaries { "ColumnName": "DataType" }
            var metadataList = new List<Dictionary<string, string>>();
            foreach (var column in columns)
            {
                metadataList.Add(new Dictionary<string, string>
                {
                    { column.ColumnName, column.DataType.ToString().ToLowerInvariant() }
                });
            }

            // Parse row data and convert to arrays
            var dataArrays = new List<List<object?>>();
            foreach (var dataRow in dataRows)
            {
                try
                {
                    var rowDict = JsonSerializer.Deserialize<Dictionary<string, object?>>(dataRow.RowDataJson)
                        ?? new Dictionary<string, object?>();
                    
                    // Convert row to array of values in column order
                    var rowArray = new List<object?>();
                    foreach (var column in columns)
                    {
                        if (rowDict.TryGetValue(column.ColumnName, out var value))
                        {
                            rowArray.Add(value);
                        }
                        else
                        {
                            rowArray.Add(null);
                        }
                    }
                    
                    dataArrays.Add(rowArray);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, $"Error parsing row data for table {table.TableName}, row {dataRow.RowIndex}");
                    throw;
                }
            }

            // Build table wrapper with name and value
            var tableWrapper = new JsonTableWrapper(
                name: table.TableName,
                value: new JsonTableValue(
                    metadata: metadataList,
                    data: dataArrays
                )
            );

            inputs.Add(tableWrapper);
        }

        return new JsonInputsOnlyResponse(inputs);
    }
}
