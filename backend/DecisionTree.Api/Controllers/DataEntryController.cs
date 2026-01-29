using DecisionTree.Api.Contracts.DataEntry;
using DecisionTree.Api.Data;
using DecisionTree.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DecisionTree.Api.Controllers;

[ApiController]
[Route("api/decision-trees/{dtId}/data")]
public class DataEntryController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<DataEntryController> _logger;

    public DataEntryController(AppDbContext db, ILogger<DataEntryController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Get all data rows for a specific table
    /// </summary>
    [HttpGet("tables/{tableId}/rows")]
    public async Task<ActionResult<List<DataRowDto>>> GetTableRows(
        int dtId, 
        int tableId, 
        CancellationToken ct)
    {
        var table = await _db.DecisionTreeTables
            .FirstOrDefaultAsync(t => t.Id == tableId && t.DecisionTreeId == dtId, ct);

        if (table is null)
            return NotFound(new { message = "Table not found" });

        var rows = await _db.DecisionTreeData
            .Where(d => d.TableId == tableId)
            .OrderBy(d => d.Id)
            .Select(d => new DataRowDto(
                d.Id,
                d.TableId,
                d.RowDataJson,
                d.RowCode,
                d.CreatedAtUtc,
                d.UpdatedAtUtc))
            .ToListAsync(ct);

        return Ok(rows);
    }

    /// <summary>
    /// Get a specific data row
    /// </summary>
    [HttpGet("tables/{tableId}/rows/{rowId}")]
    public async Task<ActionResult<DataRowDto>> GetRow(
        int dtId, 
        int tableId, 
        int rowId, 
        CancellationToken ct)
    {
        var row = await _db.DecisionTreeData
            .Where(d => d.Id == rowId && d.TableId == tableId)
            .Select(d => new DataRowDto(
                d.Id,
                d.TableId,
                d.RowDataJson,
                d.RowCode,
                d.CreatedAtUtc,
                d.UpdatedAtUtc))
            .FirstOrDefaultAsync(ct);

        if (row is null)
            return NotFound(new { message = "Row not found" });

        // Verify table belongs to decision tree
        var table = await _db.DecisionTreeTables
            .AnyAsync(t => t.Id == tableId && t.DecisionTreeId == dtId, ct);

        if (!table)
            return NotFound(new { message = "Table not found in this decision tree" });

        return Ok(row);
    }

    /// <summary>
    /// Create a new data row
    /// </summary>
    [HttpPost("tables/{tableId}/rows")]
    public async Task<ActionResult<DataRowDto>> CreateRow(
        int dtId,
        int tableId,
        [FromBody] DataRowCreateRequest request,
        CancellationToken ct)
    {
        // Verify table exists and belongs to decision tree
        var table = await _db.DecisionTreeTables
            .FirstOrDefaultAsync(t => t.Id == tableId && t.DecisionTreeId == dtId, ct);

        if (table is null)
            return NotFound(new { message = "Table not found" });

        // Validate JSON
        try
        {
            JsonDocument.Parse(request.RowDataJson);
        }
        catch (JsonException)
        {
            return BadRequest(new { message = "Invalid JSON format in RowDataJson" });
        }

        var entity = new DecisionTreeData
        {
            TableId = tableId,
            RowDataJson = request.RowDataJson,
            RowCode = request.RowCode,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.DecisionTreeData.Add(entity);
        await _db.SaveChangesAsync(ct);

        var dto = new DataRowDto(
            entity.Id,
            entity.TableId,
            entity.RowDataJson,
            entity.RowCode,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc);

        return CreatedAtAction(
            nameof(GetRow),
            new { dtId, tableId, rowId = entity.Id },
            dto);
    }

    /// <summary>
    /// Update an existing data row
    /// </summary>
    [HttpPut("tables/{tableId}/rows/{rowId}")]
    public async Task<ActionResult<DataRowDto>> UpdateRow(
        int dtId,
        int tableId,
        int rowId,
        [FromBody] DataRowUpdateRequest request,
        CancellationToken ct)
    {
        var row = await _db.DecisionTreeData
            .FirstOrDefaultAsync(d => d.Id == rowId && d.TableId == tableId, ct);

        if (row is null)
            return NotFound(new { message = "Row not found" });

        // Verify table belongs to decision tree
        var table = await _db.DecisionTreeTables
            .AnyAsync(t => t.Id == tableId && t.DecisionTreeId == dtId, ct);

        if (!table)
            return NotFound(new { message = "Table not found in this decision tree" });

        // Validate JSON
        try
        {
            JsonDocument.Parse(request.RowDataJson);
        }
        catch (JsonException)
        {
            return BadRequest(new { message = "Invalid JSON format in RowDataJson" });
        }

        row.RowDataJson = request.RowDataJson;
        row.RowCode = request.RowCode;
        // UpdatedAtUtc handled by SaveChanges override

        await _db.SaveChangesAsync(ct);

        var dto = new DataRowDto(
            row.Id,
            row.TableId,
            row.RowDataJson,
            row.RowCode,
            row.CreatedAtUtc,
            row.UpdatedAtUtc);

        return Ok(dto);
    }

    /// <summary>
    /// Delete a data row
    /// </summary>
    [HttpDelete("tables/{tableId}/rows/{rowId}")]
    public async Task<IActionResult> DeleteRow(
        int dtId,
        int tableId,
        int rowId,
        CancellationToken ct)
    {
        var row = await _db.DecisionTreeData
            .FirstOrDefaultAsync(d => d.Id == rowId && d.TableId == tableId, ct);

        if (row is null)
            return NotFound(new { message = "Row not found" });

        // Verify table belongs to decision tree
        var table = await _db.DecisionTreeTables
            .AnyAsync(t => t.Id == tableId && t.DecisionTreeId == dtId, ct);

        if (!table)
            return NotFound(new { message = "Table not found in this decision tree" });

        _db.DecisionTreeData.Remove(row);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    /// <summary>
    /// Generate JSON output with metadata + data combined
    /// </summary>
    [HttpPost("generate-json")]
    public async Task<ActionResult<JsonOutputResponse>> GenerateJson(
        int dtId,
        [FromBody] GenerateJsonRequest? request,
        CancellationToken ct)
    {
        request ??= new GenerateJsonRequest(dtId);

        var decisionTree = await _db.DecisionTrees
            .FirstOrDefaultAsync(dt => dt.Id == dtId, ct);

        if (decisionTree is null)
            return NotFound(new { message = "Decision tree not found" });

        // Get tables with columns
        var tablesQuery = _db.DecisionTreeTables
            .Include(t => t.Columns.OrderBy(c => c.OrderIndex).ThenBy(c => c.Id))
            .Where(t => t.DecisionTreeId == dtId);

        if (!request.IncludeInactiveTables)
        {
            tablesQuery = tablesQuery.Where(t => t.StatusCode == Entities.StatusCode.Active);
        }

        var tables = await tablesQuery.ToListAsync(ct);

        var tableOutputs = new List<TableJsonOutput>();

        foreach (var table in tables)
        {
            // Filter columns
            var columns = table.Columns
                .Where(c => request.IncludeInactiveColumns || c.StatusCode == Entities.StatusCode.Active)
                .OrderBy(c => c.OrderIndex)
                .ThenBy(c => c.Id)
                .ToList();

            if (columns.Count == 0)
                continue; // Skip tables with no active columns

            // Get data rows
            var dataRows = await _db.DecisionTreeData
                .Where(d => d.TableId == table.Id)
                .Select(d => d.RowDataJson)
                .ToListAsync(ct);

            // Skip empty tables
            if (dataRows.Count == 0)
                continue;

            // Parse JSON rows
            var parsedRows = new List<Dictionary<string, object?>>();
            foreach (var rowJson in dataRows)
            {
                try
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(rowJson);
                    if (dict is not null)
                        parsedRows.Add(dict);
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse row JSON for table {TableId}", table.Id);
                }
            }

            var columnMetadata = columns.Select(c => new ColumnMetadata(
                c.ColumnName,
                c.ColumnName,
                c.DataType.ToString(),
                c.IsRequired,
                c.OrderIndex
            )).ToList();

            tableOutputs.Add(new TableJsonOutput(
                table.TableName,
                table.TableName,
                table.Direction.ToString(),
                columnMetadata,
                parsedRows
            ));
        }

        var response = new JsonOutputResponse(
            decisionTree.Code,
            decisionTree.Name,
            decisionTree.SchemaVersion,
            DateTime.UtcNow,
            tableOutputs
        );

        return Ok(response);
    }

    /// <summary>
    /// Parse JSON input and store data
    /// </summary>
    [HttpPost("parse-json")]
    public async Task<ActionResult<object>> ParseJson(
        int dtId,
        [FromBody] ParseJsonRequest request,
        CancellationToken ct)
    {
        var decisionTree = await _db.DecisionTrees
            .FirstOrDefaultAsync(dt => dt.Id == dtId, ct);

        if (decisionTree is null)
            return NotFound(new { message = "Decision tree not found" });

        // Parse JSON
        JsonOutputResponse? jsonData;
        try
        {
            jsonData = JsonSerializer.Deserialize<JsonOutputResponse>(
                request.JsonContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            return BadRequest(new { message = "Invalid JSON format", error = ex.Message });
        }

        if (jsonData is null)
            return BadRequest(new { message = "Failed to parse JSON" });

        // Verify decision tree code matches
        if (jsonData.DecisionTreeCode != decisionTree.Code)
        {
            return BadRequest(new { message = "Decision tree code mismatch" });
        }

        var importedRowsCount = 0;

        foreach (var tableJson in jsonData.Tables)
        {
            // Find table by code
            var table = await _db.DecisionTreeTables
                .FirstOrDefaultAsync(t => 
                    t.DecisionTreeId == dtId && 
                    t.TableName == tableJson.TableName, ct);

            if (table is null)
            {
                _logger.LogWarning("Table {TableName} not found, skipping", tableJson.TableName);
                continue;
            }

            // Optionally clear existing data
            if (request.ReplaceExistingData)
            {
                var existingRows = _db.DecisionTreeData.Where(d => d.TableId == table.Id);
                _db.DecisionTreeData.RemoveRange(existingRows);
            }

            // Insert rows
            foreach (var rowDict in tableJson.Rows)
            {
                var rowJson = JsonSerializer.Serialize(rowDict);
                var dataRow = new DecisionTreeData
                {
                    TableId = table.Id,
                    RowDataJson = rowJson,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                _db.DecisionTreeData.Add(dataRow);
                importedRowsCount++;
            }
        }

        await _db.SaveChangesAsync(ct);

        return Ok(new
        {
            message = "JSON parsed and data imported successfully",
            importedRowsCount,
            tablesProcessed = jsonData.Tables.Count
        });
    }
}
