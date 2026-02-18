using DecisionTree.Api.Contracts.DataEntry;
using DecisionTree.Api.Data;
using DecisionTree.Api.Entities;
using DecisionTree.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text.Json;
using EntityStatusCode = DecisionTree.Api.Entities.StatusCode;

namespace DecisionTree.Api.Controllers;

[ApiController]
[Route("api/decision-trees/{dtId}/data")]
public class DataEntryController : ControllerBase
{
    private readonly AppDbContext _db; // readonly bir değişkene bir kere atanabilir ama sonradan değiştirilemez.
    private readonly ILogger<DataEntryController> _logger;
    private readonly ExcelService _excelService;
    private readonly JsonBuilderService _jsonBuilder;
    private readonly ValidationService _validator;
    private readonly ConversionService _conversionService;

    public DataEntryController(
        AppDbContext db, 
        ILogger<DataEntryController> logger,
        ExcelService excelService,
        JsonBuilderService jsonBuilder,
        ValidationService validator,
        ConversionService conversionService)
    {
        _db = db;
        _logger = logger;
        _excelService = excelService;
        _jsonBuilder = jsonBuilder;
        _validator = validator;
        _conversionService = conversionService;
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
    /// Returns complete JSON with metadata, inputs and outputs
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
    /// Export JSON with only inputs (simplified format)
    /// Returns { "inputs": [...] } without metadata or outputs
    /// GET /api/decision-trees/{dtId}/data/export-json-inputs
    /// </summary>
    [HttpGet("export-json-inputs")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JsonInputsOnlyResponse>> ExportJsonInputsOnly(
        int dtId,
        [FromQuery] bool includeInactiveTables = false,
        [FromQuery] bool includeInactiveColumns = false,
        CancellationToken ct = default)
    {
        try
        {
            var export = await _jsonBuilder.BuildJsonInputsOnlyAsync(
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
        // Detaylı validation ve logging
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { 
                message = "No file uploaded",
                code = "NO_FILE"
            });
        }

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { 
                message = $"Only .xlsx files are supported. Uploaded file: {file.FileName}",
                code = "INVALID_FORMAT",
                uploadedFileName = file.FileName
            });
        }

        var uploadedFileName = Path.GetFileName(file.FileName);
        if (uploadedFileName.StartsWith("~$", StringComparison.Ordinal))
        {
            return BadRequest(new
            {
                message = "Temporary Excel lock file selected. Please select the real workbook file.",
                code = "TEMP_LOCK_FILE",
                uploadedFileName = file.FileName
            });
        }

        if (!await IsValidXlsxPackageAsync(file, ct))
        {
            return BadRequest(new
            {
                message = "Invalid .xlsx package. Please save the file as 'Excel Workbook (*.xlsx)' and try again.",
                code = "INVALID_XLSX_PACKAGE",
                uploadedFileName = file.FileName
            });
        }

        // Verify decision tree exists
        var decisionTree = await _db.DecisionTrees
            .FirstOrDefaultAsync(dt => dt.Id == dtId, ct);

        if (decisionTree is null)
        {
            return NotFound(new { 
                message = $"Decision tree with ID {dtId} not found",
                code = "TREE_NOT_FOUND"
            });
        }

        // Get tables with columns
        var tables = await _db.DecisionTreeTables
            .Include(t => t.Columns.Where(c => c.StatusCode == EntityStatusCode.Active))
            .Where(t => t.DecisionTreeId == dtId && t.StatusCode == EntityStatusCode.Active)
            .ToListAsync(ct);

        if (tables.Count == 0)
        {
            return BadRequest(new { 
                message = "No active tables found for this decision tree",
                code = "NO_TABLES"
            });
        }

        // Read Excel file
        ExcelReadResult excelResult;
        
        try
        {
            using (var stream = file.OpenReadStream())
            {
                excelResult = await _excelService.ReadExcelAsync(stream, tables, ct);
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = $"Error reading Excel file: {ex.Message}",
                code = "EXCEL_READ_ERROR",
                details = ex.InnerException?.Message
            });
        }

        if (!excelResult.Success)
        {
            return BadRequest(new
            {
                message = "Failed to read Excel file",
                code = "EXCEL_PARSE_ERROR",
                errors = excelResult.Errors,
                warnings = excelResult.Warnings
            });
        }

        if (excelResult.TableData.Count == 0)
        {
            return BadRequest(new
            {
                message = "No data found in Excel file worksheets",
                code = "NO_DATA_FOUND",
                warnings = excelResult.Warnings
            });
        }

        // Import data into database
        var importedRowsCount = 0;
        var allErrors = new List<string>();

        try
        {
            foreach (var (tableName, tableData) in excelResult.TableData)
            {
                var table = tables.FirstOrDefault(t => t.TableName == tableName);
                if (table == null)
                {
                    allErrors.Add($"Table '{tableName}' not found in database");
                    continue;
                }

                // 6.2.2: Get unique identifier column for smart row matching
                var uniqueIdColumn = tableData.UniqueIdentifierColumnName ?? "";
                var hasUniqueIdentifier = !string.IsNullOrWhiteSpace(uniqueIdColumn);

                // 6.2.3: Handle replace vs merge strategy
                if (replaceExisting)
                {
                    // REPLACE STRATEGY: Delete all existing data and insert new rows
                    _logger.LogInformation(
                        "Replacing all data in table '{TableName}' (total rows to insert: {RowCount})",
                        tableName, tableData.Rows.Count);
                    
                    var existingRows = _db.DecisionTreeData.Where(d => d.TableId == table.Id);
                    _db.DecisionTreeData.RemoveRange(existingRows);
                }
                else
                {
                    // MERGE STRATEGY: Update matching rows, insert new ones
                    if (hasUniqueIdentifier)
                    {
                        _logger.LogInformation(
                            "Merging data in table '{TableName}' using unique identifier '{UIDColumn}'",
                            tableName, uniqueIdColumn);
                        
                        // Get existing data
                        var existingDataRows = await _db.DecisionTreeData
                            .Where(d => d.TableId == table.Id)
                            .ToListAsync(ct);

                        foreach (var newRowDict in tableData.Rows)
                        {
                            if (!newRowDict.TryGetValue(uniqueIdColumn, out var newUidValue) || newUidValue == null)
                            {
                                allErrors.Add(
                                    $"Row missing unique identifier column '{uniqueIdColumn}' - skipping this row");
                                continue;
                            }

                            // Find matching existing row by unique identifier
                            var matchingExistingRow = existingDataRows.FirstOrDefault(row =>
                            {
                                try
                                {
                                    var existingData = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                                        row.RowDataJson) ?? new();
                                    return existingData.TryGetValue(uniqueIdColumn, out var existingUidValue) &&
                                           existingUidValue?.ToString() == newUidValue.ToString();
                                }
                                catch
                                {
                                    return false;
                                }
                            });

                            if (matchingExistingRow != null)
                            {
                                // UPDATE: Replace existing row's JSON
                                matchingExistingRow.RowDataJson = JsonSerializer.Serialize(newRowDict);
                                matchingExistingRow.UpdatedAtUtc = DateTime.UtcNow;
                                _logger.LogDebug("Updated row with {UIDColumn}={UIDValue} in table '{TableName}'",
                                    uniqueIdColumn, newUidValue, tableName);
                            }
                            else
                            {
                                // INSERT: New row
                                var newDataRow = new DecisionTreeData
                                {
                                    TableId = table.Id,
                                    RowDataJson = JsonSerializer.Serialize(newRowDict),
                                    CreatedAtUtc = DateTime.UtcNow,
                                    UpdatedAtUtc = DateTime.UtcNow
                                };
                                _db.DecisionTreeData.Add(newDataRow);
                                _logger.LogDebug("Inserted new row with {UIDColumn}={UIDValue} in table '{TableName}'",
                                    uniqueIdColumn, newUidValue, tableName);
                            }

                            importedRowsCount++;
                        }
                    }
                    else
                    {
                        // No unique identifier - warn user and insert as new rows
                        _logger.LogWarning(
                            "Table '{TableName}' has no unique identifier column defined. " +
                            "Import will only INSERT mode (not merge). Set IsUniqueIdentifier on a column for merge support.",
                            tableName);
                        
                        allErrors.Add(
                            $"Table '{tableName}' has no unique identifier. " +
                            $"Rows are being inserted as new (not merged with existing data). " +
                            $"Set IsUniqueIdentifier=true on one column to enable merge mode.");

                        // Insert as new rows
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
                    continue;
                }

                // REPLACE MODE: Insert all new rows
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving imported data to database");
            return BadRequest(new
            {
                message = $"Error saving data to database: {ex.Message}",
                code = "DATABASE_ERROR",
                details = ex.InnerException?.Message,
                importedRowsBeforeError = importedRowsCount
            });
        }

        return Ok(new
        {
            message = replaceExisting 
                ? "Excel imported successfully (REPLACE mode - all data replaced)" 
                : "Excel imported successfully (MERGE mode - rows updated/inserted)",
            importedRowsCount,
            tablesProcessed = excelResult.TableData.Count,
            warnings = excelResult.Warnings,
            errors = allErrors.Concat(
                excelResult.TableData.SelectMany(td => td.Value.Errors)
            ).ToList()
        });
    }

    /// <summary>
    /// Import data from CSV file
    /// POST /api/decision-trees/{dtId}/data/import-csv
    /// </summary>
    [HttpPost("import-csv")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB limit
    public async Task<ActionResult<object>> ImportCsv(
        int dtId,
        IFormFile file,
        [FromQuery] bool replaceExisting = false,
        CancellationToken ct = default)
    {
        // Validation
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { 
                message = "No file uploaded",
                code = "NO_FILE"
            });
        }

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { 
                message = $"Only .csv files are supported. Uploaded file: {file.FileName}",
                code = "INVALID_FORMAT",
                uploadedFileName = file.FileName
            });
        }

        // Verify decision tree exists
        var decisionTree = await _db.DecisionTrees
            .FirstOrDefaultAsync(dt => dt.Id == dtId, ct);

        if (decisionTree is null)
        {
            return NotFound(new { 
                message = $"Decision tree with ID {dtId} not found",
                code = "TREE_NOT_FOUND"
            });
        }

        // Get tables with columns
        var tables = await _db.DecisionTreeTables
            .Include(t => t.Columns.Where(c => c.StatusCode == EntityStatusCode.Active))
            .Where(t => t.DecisionTreeId == dtId && t.StatusCode == EntityStatusCode.Active)
            .ToListAsync(ct);

        if (tables.Count == 0)
        {
            return BadRequest(new { 
                message = "No active tables found for this decision tree",
                code = "NO_TABLES"
            });
        }

        // Read CSV file
        ExcelReadResult csvResult;
        
        try
        {
            using (var stream = file.OpenReadStream())
            {
                csvResult = await _excelService.ReadCsvAsync(stream, tables, ct);
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                message = $"Error reading CSV file: {ex.Message}",
                code = "CSV_READ_ERROR",
                details = ex.InnerException?.Message
            });
        }

        if (!csvResult.Success)
        {
            return BadRequest(new
            {
                message = "Failed to read CSV file",
                code = "CSV_PARSE_ERROR",
                errors = csvResult.Errors,
                warnings = csvResult.Warnings
            });
        }

        if (csvResult.TableData.Count == 0)
        {
            return BadRequest(new
            {
                message = "No data found in CSV file",
                code = "NO_DATA_FOUND",
                warnings = csvResult.Warnings
            });
        }

        // Import data into database
        var importedRowsCount = 0;
        var allErrors = new List<string>();

        try
        {
            foreach (var (tableName, tableData) in csvResult.TableData)
            {
                var table = tables.FirstOrDefault(t => t.TableName == tableName);
                if (table == null)
                {
                    allErrors.Add($"Table '{tableName}' not found in database");
                    continue;
                }

                // Get unique identifier column for smart row matching
                var uniqueIdColumn = tableData.UniqueIdentifierColumnName ?? "";
                var hasUniqueIdentifier = !string.IsNullOrWhiteSpace(uniqueIdColumn);

                // Handle replace vs merge strategy
                if (replaceExisting)
                {
                    // REPLACE STRATEGY: Delete all existing data and insert new rows
                    _logger.LogInformation(
                        "Replacing all data in table '{TableName}' (total rows to insert: {RowCount})",
                        tableName, tableData.Rows.Count);
                    
                    var existingRows = _db.DecisionTreeData.Where(d => d.TableId == table.Id);
                    _db.DecisionTreeData.RemoveRange(existingRows);
                }
                else
                {
                    // MERGE STRATEGY: Update matching rows, insert new ones
                    if (hasUniqueIdentifier)
                    {
                        _logger.LogInformation(
                            "Merging data in table '{TableName}' using unique identifier '{UIDColumn}'",
                            tableName, uniqueIdColumn);
                        
                        // Get existing data
                        var existingDataRows = await _db.DecisionTreeData
                            .Where(d => d.TableId == table.Id)
                            .ToListAsync(ct);

                        foreach (var newRowDict in tableData.Rows)
                        {
                            if (!newRowDict.TryGetValue(uniqueIdColumn, out var newUidValue) || newUidValue == null)
                            {
                                allErrors.Add(
                                    $"Row missing unique identifier column '{uniqueIdColumn}' - skipping this row");
                                continue;
                            }

                            // Find matching existing row by unique identifier
                            var matchingExistingRow = existingDataRows.FirstOrDefault(row =>
                            {
                                try
                                {
                                    var existingData = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                                        row.RowDataJson) ?? new();
                                    return existingData.TryGetValue(uniqueIdColumn, out var existingUidValue) &&
                                           existingUidValue?.ToString() == newUidValue.ToString();
                                }
                                catch
                                {
                                    return false;
                                }
                            });

                            if (matchingExistingRow != null)
                            {
                                // UPDATE: Replace existing row's JSON
                                matchingExistingRow.RowDataJson = JsonSerializer.Serialize(newRowDict);
                                matchingExistingRow.UpdatedAtUtc = DateTime.UtcNow;
                            }
                            else
                            {
                                // INSERT: New row
                                var newDataRow = new DecisionTreeData
                                {
                                    TableId = table.Id,
                                    RowDataJson = JsonSerializer.Serialize(newRowDict),
                                    CreatedAtUtc = DateTime.UtcNow,
                                    UpdatedAtUtc = DateTime.UtcNow
                                };
                                _db.DecisionTreeData.Add(newDataRow);
                            }

                            importedRowsCount++;
                        }
                    }
                    else
                    {
                        // No unique identifier - warn user and insert as new rows
                        _logger.LogWarning(
                            "Table '{TableName}' has no unique identifier column defined. " +
                            "Import will only INSERT mode (not merge). Set IsUniqueIdentifier on a column for merge support.",
                            tableName);
                        
                        allErrors.Add(
                            $"Table '{tableName}' has no unique identifier. " +
                            $"Rows are being inserted as new (not merged with existing data). " +
                            $"Set IsUniqueIdentifier=true on one column to enable merge mode.");

                        // Insert as new rows
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
                    continue;
                }

                // REPLACE MODE: Insert all new rows
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving imported CSV data to database");
            return BadRequest(new
            {
                message = $"Error saving data to database: {ex.Message}",
                code = "DATABASE_ERROR",
                details = ex.InnerException?.Message,
                importedRowsBeforeError = importedRowsCount
            });
        }

        return Ok(new
        {
            message = replaceExisting 
                ? "CSV imported successfully (REPLACE mode - all data replaced)" 
                : "CSV imported successfully (MERGE mode - rows updated/inserted)",
            importedRowsCount,
            tablesProcessed = csvResult.TableData.Count,
            warnings = csvResult.Warnings,
            errors = allErrors.Concat(
                csvResult.TableData.SelectMany(td => td.Value.Errors)
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

    /// <summary>
    /// Export data as CSV file (universal format compatibility)
    /// GET /api/decision-trees/{dtId}/data/export-csv
    /// </summary>
    [HttpGet("export-csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExportCsv(
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

        // Generate CSV file
        var csvBytes = await _excelService.WriteCsvAsync(tableData, tables, ct);

        var fileName = $"{decisionTree.Code}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

        return File(csvBytes, "text/csv", fileName);
    }

    /// <summary>
    /// Download Excel template for data entry (empty structure)
    /// GET /api/decision-trees/{dtId}/data/template-excel
    /// </summary>
    [HttpGet("template-excel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadExcelTemplate(
        int dtId,
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
        var tables = await _db.DecisionTreeTables
            .Include(t => t.Columns)
            .Where(t => t.DecisionTreeId == dtId && t.StatusCode == EntityStatusCode.Active)
            .ToListAsync(ct);

        if (tables.Count == 0)
        {
            return BadRequest(new { message = "No tables found for this decision tree" });
        }

        // Create empty table data (just headers, no rows)
        var emptyTableData = new Dictionary<string, TableDataResult>();
        foreach (var table in tables)
        {
            var columns = table.Columns
                .Where(c => c.StatusCode == EntityStatusCode.Active)
                .OrderBy(c => c.OrderIndex)
                .ThenBy(c => c.Id)
                .ToList();

            if (columns.Count > 0)
            {
                // Create empty row data structure (no actual rows, just structure)
                emptyTableData[table.TableName] = new TableDataResult { Rows = new List<Dictionary<string, object?>>() };
            }
        }

        // Generate Excel template (headers only, no data)
        var excelBytes = await _excelService.WriteExcelAsync(emptyTableData, tables, ct);

        var fileName = $"{decisionTree.Code}_TEMPLATE.xlsx";

        return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Convert uploaded Excel file to JSON format
    /// Accepts an Excel file and returns JSON representation
    /// </summary>
    [HttpPost("convert/excel-to-json")]
    public async Task<ActionResult<object>> ConvertExcelToJson(
        int dtId,
        IFormFile file,
        CancellationToken ct)
    {
        try
        {
            // Validate file
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file provided" });

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { message = "Only .xlsx files are supported" });

            // Convert
            using var stream = file.OpenReadStream();
            var result = await _conversionService.ExcelToJsonAsync(dtId, stream, ct);

            if (!result.Success)
                return BadRequest(new { message = "Conversion failed", errors = result.Errors });

            return Ok(new
            {
                success = result.Success,
                json = result.JsonContent,
                warnings = result.Warnings
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExcelToJson endpoint");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Convert JSON string to Excel file format
    /// User provides JSON data, gets .xlsx file
    /// </summary>
    [HttpPost("convert/json-string-to-excel")]
    public async Task<ActionResult> ConvertJsonStringToExcel(
        int dtId,
        [FromBody] JsonConvertRequest request,
        CancellationToken ct)
    {
        try
        {
            var result = await _conversionService.JsonStringToExcelAsync(dtId, request.JsonData, ct);

            if (!result.Success)
                return BadRequest(new { message = "Conversion failed", errors = result.Errors });

            return File(
                result.FileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ConvertJsonStringToExcel endpoint");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    /// <summary>
    /// Convert decision tree data to Excel file format
    /// Retrieves data from database and exports as .xlsx
    /// </summary>
    [HttpPost("convert/json-to-excel")]
    public async Task<ActionResult> ConvertJsonToExcel(
        int dtId,
        CancellationToken ct)
    {
        try
        {
            var result = await _conversionService.JsonToExcelAsync(dtId, ct);

            if (!result.Success)
                return BadRequest(new { message = "Conversion failed", errors = result.Errors });

            return File(
                result.FileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in JsonToExcel endpoint");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message });
        }
    }

    private static async Task<bool> IsValidXlsxPackageAsync(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        if (!stream.CanRead || stream.Length < 4)
            return false;

        var signature = new byte[4];
        var read = await stream.ReadAsync(signature.AsMemory(0, 4), ct);
        if (read < 4)
            return false;

        // ZIP signatures used by OpenXML (.xlsx) files
        var isZipSignature =
            (signature[0] == 0x50 && signature[1] == 0x4B && signature[2] == 0x03 && signature[3] == 0x04) ||
            (signature[0] == 0x50 && signature[1] == 0x4B && signature[2] == 0x05 && signature[3] == 0x06) ||
            (signature[0] == 0x50 && signature[1] == 0x4B && signature[2] == 0x07 && signature[3] == 0x08);

        if (!isZipSignature)
            return false;

        try
        {
            stream.Position = 0;
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            var hasContentTypes = archive.GetEntry("[Content_Types].xml") is not null;
            var hasWorkbook = archive.GetEntry("xl/workbook.xml") is not null;
            return hasContentTypes && hasWorkbook;
        }
        catch
        {
            return false;
        }
    }
}
