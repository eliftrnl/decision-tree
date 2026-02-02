using DecisionTree.Api.Contracts.DataEntry;
using DecisionTree.Api.Data;
using DecisionTree.Api.Entities;
using DecisionTree.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using EntityStatusCode = DecisionTree.Api.Entities.StatusCode;

namespace DecisionTree.Api.Controllers;

[ApiController]
[Route("api/decision-trees/{dtId}/data")]
public class DataEntryController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<DataEntryController> _logger;
    private readonly ExcelService _excelService;
    private readonly JsonBuilderService _jsonBuilder;
    private readonly ValidationService _validator;

    public DataEntryController(
        AppDbContext db, 
        ILogger<DataEntryController> logger,
        ExcelService excelService,
        JsonBuilderService jsonBuilder,
        ValidationService validator)
    {
        _db = db;
        _logger = logger;
        _excelService = excelService;
        _jsonBuilder = jsonBuilder;
        _validator = validator;
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
            .OrderBy(d => d.RowIndex)
            .Select(d => new DataRowDto(
                d.Id,
                d.TableId,
                d.RowIndex,
                d.RowDataJson,
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
                d.RowIndex,
                d.RowDataJson,
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
            DecisionTreeId = dtId,
            TableId = tableId,
            RowIndex = request.RowIndex,
            RowDataJson = request.RowDataJson,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _db.DecisionTreeData.Add(entity);
        await _db.SaveChangesAsync(ct);

        var dto = new DataRowDto(
            entity.Id,
            entity.TableId,
            entity.RowIndex,
            entity.RowDataJson,
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
        row.RowIndex = request.RowIndex;
        // UpdatedAtUtc handled by SaveChanges override

        await _db.SaveChangesAsync(ct);

        var dto = new DataRowDto(
            row.Id,
            row.TableId,
            row.RowIndex,
            row.RowDataJson,
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
    /// Export JSON using JsonBuilderService (newer version)
    /// Returns complete JSON with metadata and data
    /// Automatically skips tables with no data
    /// </summary>
    [HttpGet("export-json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JsonExportResponse>> ExportJson(
        int dtId,
        [FromQuery] bool includeInactiveTables = false,
        [FromQuery] bool includeInactiveColumns = false,
        CancellationToken ct = default)
    {
        try
        {
            var export = await _jsonBuilder.BuildJsonExportAsync(
                dtId,
                includeInactiveTables,
                includeInactiveColumns,
                ct);

            return Ok(export);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Export JSON as formatted string
    /// GET /api/decision-trees/{dtId}/data/export-json-string
    /// </summary>
    [HttpGet("export-json-string")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ExportJsonString(
        int dtId,
        [FromQuery] bool includeInactiveTables = false,
        [FromQuery] bool includeInactiveColumns = false,
        CancellationToken ct = default)
    {
        try
        {
            var jsonString = await _jsonBuilder.BuildAndSerializeJsonAsync(
                dtId,
                includeInactiveTables,
                includeInactiveColumns,
                ct);

            return Content(jsonString, "application/json");
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Generate JSON output with metadata + data combined (legacy)
    /// </summary>
    [HttpPost("generate-json")]
    [Obsolete("Use ExportJson instead")]
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

    /// <summary>
    /// Import data from Excel file
    /// POST /api/decision-trees/{dtId}/data/import-excel
    /// </summary>
    [HttpPost("import-excel")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
    public async Task<ActionResult<object>> ImportExcel(
        int dtId,
        IFormFile file,
        [FromQuery] bool replaceExisting = false,
        CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file uploaded" });
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Only .xlsx files are supported" });
        }

        // Verify decision tree exists
        var decisionTree = await _db.DecisionTrees
            .FirstOrDefaultAsync(dt => dt.Id == dtId, ct);

        if (decisionTree is null)
        {
            return NotFound(new { message = "Decision tree not found" });
        }

        // Get tables with columns
        var tables = await _db.DecisionTreeTables
            .Include(t => t.Columns.Where(c => c.StatusCode == EntityStatusCode.Active))
            .Where(t => t.DecisionTreeId == dtId && t.StatusCode == EntityStatusCode.Active)
            .ToListAsync(ct);

        if (tables.Count == 0)
        {
            return BadRequest(new { message = "No active tables found for this decision tree" });
        }

        // Read Excel file
        ExcelReadResult excelResult;
        
        using (var stream = file.OpenReadStream())
        {
            excelResult = await _excelService.ReadExcelAsync(stream, tables, ct);
        }

        if (!excelResult.Success)
        {
            return BadRequest(new
            {
                message = "Failed to read Excel file",
                errors = excelResult.Errors
            });
        }

        // Import data into database
        var importedRowsCount = 0;

        foreach (var (tableName, tableData) in excelResult.TableData)
        {
            var table = tables.FirstOrDefault(t => t.TableName == tableName);
            if (table == null) continue;

            // Optionally clear existing data
            if (replaceExisting)
            {
                var existingRows = _db.DecisionTreeData.Where(d => d.TableId == table.Id);
                _db.DecisionTreeData.RemoveRange(existingRows);
            }

            // Insert rows
            foreach (var rowDict in tableData.Rows)
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
            message = "Excel imported successfully",
            importedRowsCount,
            tablesProcessed = excelResult.TableData.Count,
            warnings = excelResult.Warnings,
            errors = excelResult.Errors.Concat(
                excelResult.TableData.SelectMany(td => td.Value.Errors)
            ).ToList()
        });
    }

    /// <summary>
    /// Export data to Excel file
    /// GET /api/decision-trees/{dtId}/data/export-excel
    /// </summary>
    [HttpGet("export-excel")]
    public async Task<IActionResult> ExportExcel(
        int dtId,
        [FromQuery] bool includeInactiveTables = false,
        [FromQuery] bool includeInactiveColumns = false,
        CancellationToken ct = default)
    {
        // Verify decision tree exists
        var decisionTree = await _db.DecisionTrees
            .FirstOrDefaultAsync(dt => dt.Id == dtId, ct);

        if (decisionTree is null)
        {
            return NotFound(new { message = "Decision tree not found" });
        }

        // Get tables with columns
        var tablesQuery = _db.DecisionTreeTables
            .Include(t => t.Columns)
            .Where(t => t.DecisionTreeId == dtId);

        if (!includeInactiveTables)
        {
            tablesQuery = tablesQuery.Where(t => t.StatusCode == EntityStatusCode.Active);
        }

        var tables = await tablesQuery.ToListAsync(ct);

        if (tables.Count == 0)
        {
            return BadRequest(new { message = "No tables found for this decision tree" });
        }

        // Prepare table data
        var tableData = new Dictionary<string, TableDataResult>();

        foreach (var table in tables)
        {
            // Filter columns
            var columns = table.Columns
                .Where(c => includeInactiveColumns || c.StatusCode == EntityStatusCode.Active)
                .OrderBy(c => c.OrderIndex)
                .ThenBy(c => c.Id)
                .ToList();

            if (columns.Count == 0)
                continue;

            // Get data rows
            var dataRows = await _db.DecisionTreeData
                .Where(d => d.TableId == table.Id)
                .Select(d => d.RowDataJson)
                .ToListAsync(ct);

            if (dataRows.Count == 0)
                continue; // Skip tables with no data

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

            tableData[table.TableName] = new TableDataResult { Rows = parsedRows };
        }

        if (tableData.Count == 0)
        {
            return BadRequest(new { message = "No data found to export" });
        }

        // Generate Excel file
        var excelBytes = await _excelService.WriteExcelAsync(tableData, tables, ct);

        var fileName = $"{decisionTree.Code}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Validate a row against column metadata without saving
    /// POST /api/decision-trees/{dtId}/data/validate-row
    /// </summary>
    [HttpPost("validate-row")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ValidateRowResponse>> ValidateRow(
        int dtId,
        [FromBody] ValidateRowRequest request,
        CancellationToken ct = default)
    {
        // Verify table exists and belongs to decision tree
        var table = await _db.DecisionTreeTables
            .FirstOrDefaultAsync(t => t.Id == request.TableId && t.DecisionTreeId == dtId, ct);

        if (table is null)
            return NotFound(new { message = "Table not found in this decision tree" });

        // Validate using ValidationService
        var validationResult = await _validator.ValidateRowAsync(
            request.TableId,
            request.RowData,
            0, // row index for error reporting
            ct);

        var response = new ValidateRowResponse(
            IsValid: validationResult.IsValid,
            Errors: validationResult.Errors,
            Warnings: validationResult.Warnings
        );

        return Ok(response);
    }
}
