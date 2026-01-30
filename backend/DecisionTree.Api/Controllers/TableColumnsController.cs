using DecisionTree.Api.Contracts.DecisionTrees;
using DecisionTree.Api.Data;
using DecisionTree.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EntityStatusCode = DecisionTree.Api.Entities.StatusCode;

namespace DecisionTree.Api.Controllers;

[ApiController]
[Route("api/decision-trees/{dtId}/tables/{tableId}/columns")]
public class TableColumnsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TableColumnsController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/decision-trees/{dtId}/tables/{tableId}/columns
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<TableColumnDto>>> GetAll(
        int dtId,
        int tableId,
        CancellationToken ct)
    {
        var tableExists = await _db.DecisionTreeTables
            .AnyAsync(x => x.Id == tableId && x.DecisionTreeId == dtId, ct);

        if (!tableExists)
            return NotFound(new { message = "Table not found" });

        var columns = await _db.TableColumns
            .AsNoTracking()
            .Where(x => x.TableId == tableId)
            .OrderBy(x => x.OrderIndex)
            .ThenBy(x => x.Id)
            .Select(x => new TableColumnDto(
                x.Id,
                x.TableId,
                x.ColumnName,
                x.ExcelHeaderName,
                x.Description,
                x.DataType.ToString(),
                x.IsRequired,
                x.StatusCode.ToString(),
                x.OrderIndex,
                x.Format,
                x.MaxLength,
                x.Precision,
                x.Scale,
                x.ValidFrom,
                x.ValidTo
            ))
            .ToListAsync(ct);

        return Ok(columns);
    }

    // GET /api/decision-trees/{dtId}/tables/{tableId}/columns/{id}
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TableColumnDto>> GetById(
        int dtId,
        int tableId,
        int id,
        CancellationToken ct)
    {
        var column = await _db.TableColumns
            .AsNoTracking()
            .Where(x => x.Id == id && x.TableId == tableId)
            .Select(x => new TableColumnDto(
                x.Id,
                x.TableId,
                x.ColumnName,
                x.ExcelHeaderName,
                x.Description,
                x.DataType.ToString(),
                x.IsRequired,
                x.StatusCode.ToString(),
                x.OrderIndex,
                x.Format,
                x.MaxLength,
                x.Precision,
                x.Scale,
                x.ValidFrom,
                x.ValidTo
            ))
            .FirstOrDefaultAsync(ct);

        if (column == null)
            return NotFound(new { message = "Column not found" });

        return Ok(column);
    }

    // POST /api/decision-trees/{dtId}/tables/{tableId}/columns
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TableColumnDto>> Create(
        int dtId,
        int tableId,
        [FromBody] TableColumnCreateRequest request,
        CancellationToken ct)
    {
        if (request.TableId != tableId)
            return BadRequest(new { message = "TableId mismatch" });

        var tableExists = await _db.DecisionTreeTables
            .AnyAsync(x => x.Id == tableId && x.DecisionTreeId == dtId, ct);

        if (!tableExists)
            return NotFound(new { message = "Table not found" });

        // Check for duplicate ColumnName within same Table
        var nameExists = await _db.TableColumns
            .AnyAsync(x => x.TableId == tableId && x.ColumnName == request.ColumnName, ct);

        if (nameExists)
            return BadRequest(new { message = $"Column name '{request.ColumnName}' already exists in this table" });

        var column = new TableColumn
        {
            TableId = request.TableId,
            ColumnName = request.ColumnName,
            ExcelHeaderName = request.ExcelHeaderName,
            Description = request.Description,
            DataType = (ColumnDataType)request.DataType,
            IsRequired = request.IsRequired,
            StatusCode = request.StatusCode == 0 ? EntityStatusCode.Active : (EntityStatusCode)request.StatusCode,
            OrderIndex = request.OrderIndex,
            Format = request.Format,
            MaxLength = request.MaxLength,
            Precision = request.Precision,
            Scale = request.Scale,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo
        };

        _db.TableColumns.Add(column);
        await _db.SaveChangesAsync(ct);

        var dto = new TableColumnDto(
            column.Id,
            column.TableId,
            column.ColumnName,
            column.ExcelHeaderName,
            column.Description,
            column.DataType.ToString(),
            column.IsRequired,
            column.StatusCode.ToString(),
            column.OrderIndex,
            column.Format,
            column.MaxLength,
            column.Precision,
            column.Scale,
            column.ValidFrom,
            column.ValidTo
        );

        return CreatedAtAction(nameof(GetById), new { dtId, tableId, id = column.Id }, dto);
    }

    // PUT /api/decision-trees/{dtId}/tables/{tableId}/columns/{id}
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int dtId,
        int tableId,
        int id,
        [FromBody] TableColumnUpdateRequest request,
        CancellationToken ct)
    {
        var column = await _db.TableColumns
            .FirstOrDefaultAsync(x => x.Id == id && x.TableId == tableId, ct);

        if (column == null)
            return NotFound(new { message = "Column not found" });

        // Check for duplicate ColumnName (excluding current column)
        var nameExists = await _db.TableColumns
            .AnyAsync(x => x.TableId == tableId && x.ColumnName == request.ColumnName && x.Id != id, ct);

        if (nameExists)
            return BadRequest(new { message = $"Column name '{request.ColumnName}' already exists in this table" });

        column.ColumnName = request.ColumnName;
        column.ExcelHeaderName = request.ExcelHeaderName;
        column.Description = request.Description;
        column.DataType = (ColumnDataType)request.DataType;
        column.IsRequired = request.IsRequired;
        column.StatusCode = (StatusCode)request.StatusCode;
        column.OrderIndex = request.OrderIndex;
        column.Format = request.Format;
        column.MaxLength = request.MaxLength;
        column.Precision = request.Precision;
        column.Scale = request.Scale;
        column.ValidFrom = request.ValidFrom;
        column.ValidTo = request.ValidTo;

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // DELETE /api/decision-trees/{dtId}/tables/{tableId}/columns/{id}
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int dtId,
        int tableId,
        int id,
        CancellationToken ct)
    {
        var column = await _db.TableColumns
            .FirstOrDefaultAsync(x => x.Id == id && x.TableId == tableId, ct);

        if (column == null)
            return NotFound(new { message = "Column not found" });

        _db.TableColumns.Remove(column);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // PATCH /api/decision-trees/{dtId}/tables/{tableId}/columns/reorder
    [HttpPatch("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reorder(
        int dtId,
        int tableId,
        [FromBody] ReorderColumnsRequest request,
        CancellationToken ct)
    {
        var tableExists = await _db.DecisionTreeTables
            .AnyAsync(x => x.Id == tableId && x.DecisionTreeId == dtId, ct);

        if (!tableExists)
            return NotFound(new { message = "Table not found" });

        var columns = await _db.TableColumns
            .Where(x => x.TableId == tableId && request.ColumnIds.Contains(x.Id))
            .ToListAsync(ct);

        if (columns.Count != request.ColumnIds.Count)
            return BadRequest(new { message = "Some column IDs are invalid" });

        for (int i = 0; i < request.ColumnIds.Count; i++)
        {
            var column = columns.First(c => c.Id == request.ColumnIds[i]);
            column.OrderIndex = i;
        }

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
