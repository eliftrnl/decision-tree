using DecisionTree.Api.Contracts.DecisionTrees;
using DecisionTree.Api.Data;
using DecisionTree.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DecisionTree.Api.Controllers;

[ApiController]
[Route("api/decision-trees/{dtId}/tables")]
public class DecisionTreeTablesController : ControllerBase
{
    private readonly AppDbContext _db;

    public DecisionTreeTablesController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/decision-trees/{dtId}/tables
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<DecisionTreeTableDto>>> GetAll(
        int dtId,
        CancellationToken ct)
    {
        var dtExists = await _db.DecisionTrees.AnyAsync(x => x.Id == dtId, ct);
        if (!dtExists)
            return NotFound(new { message = "Decision tree not found" });

        var tables = await _db.DecisionTreeTables
            .AsNoTracking()
            .Where(x => x.DecisionTreeId == dtId)
            .OrderBy(x => x.Id)
            .Select(x => new DecisionTreeTableDto(
                x.Id,
                x.DecisionTreeId,
                x.TableName,
                x.Direction.ToString(),
                x.StatusCode.ToString()
            ))
            .ToListAsync(ct);

        return Ok(tables);
    }

    // GET /api/decision-trees/{dtId}/tables/{id}
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionTreeTableDto>> GetById(
        int dtId,
        int id,
        CancellationToken ct)
    {
        var table = await _db.DecisionTreeTables
            .AsNoTracking()
            .Where(x => x.Id == id && x.DecisionTreeId == dtId)
            .Select(x => new DecisionTreeTableDto(
                x.Id,
                x.DecisionTreeId,
                x.TableName,
                x.Direction.ToString(),
                x.StatusCode.ToString()
            ))
            .FirstOrDefaultAsync(ct);

        if (table == null)
            return NotFound(new { message = "Table not found" });

        return Ok(table);
    }

    // POST /api/decision-trees/{dtId}/tables
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionTreeTableDto>> Create(
        int dtId,
        [FromBody] DecisionTreeTableCreateRequest request,
        CancellationToken ct)
    {
        if (request.DecisionTreeId != 0 && request.DecisionTreeId != dtId)
            return BadRequest(new { message = "DecisionTreeId mismatch" });

        if (!TryParseDirection(request.Direction, out var direction))
            return BadRequest(new { message = "Invalid direction. Use Input/Output or 1/2." });

        if (!Enum.IsDefined(typeof(StatusCode), request.StatusCode))
            return BadRequest(new { message = "Invalid statusCode. Use 1 (Active) or 2 (Passive)." });

        var dtExists = await _db.DecisionTrees.AnyAsync(x => x.Id == dtId, ct);
        if (!dtExists)
            return NotFound(new { message = "Decision tree not found" });

        // Check for duplicate TableName within same DecisionTree
        var codeExists = await _db.DecisionTreeTables
            .AnyAsync(x => x.DecisionTreeId == dtId && x.TableName == request.TableName, ct);

        if (codeExists)
            return BadRequest(new { message = $"Table name '{request.TableName}' already exists" });

        var table = new DecisionTreeTable
        {
            DecisionTreeId = dtId,
            TableName = request.TableName,
            Direction = direction,
            StatusCode = (StatusCode)request.StatusCode
        };

        _db.DecisionTreeTables.Add(table);
        await _db.SaveChangesAsync(ct);

        var dto = new DecisionTreeTableDto(
            table.Id,
            table.DecisionTreeId,
            table.TableName,
            table.Direction.ToString(),
            table.StatusCode.ToString()
        );

        return CreatedAtAction(nameof(GetById), new { dtId, id = table.Id }, dto);
    }

    // PUT /api/decision-trees/{dtId}/tables/{id}
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        int dtId,
        int id,
        [FromBody] DecisionTreeTableUpdateRequest request,
        CancellationToken ct)
    {
        var table = await _db.DecisionTreeTables
            .FirstOrDefaultAsync(x => x.Id == id && x.DecisionTreeId == dtId, ct);

        if (table == null)
            return NotFound(new { message = "Table not found" });

        // Check for duplicate TableName (excluding current table)
        var codeExists = await _db.DecisionTreeTables
            .AnyAsync(x => x.DecisionTreeId == dtId && x.TableName == request.TableName && x.Id != id, ct);

        if (codeExists)
            return BadRequest(new { message = $"Table name '{request.TableName}' already exists" });

        table.TableName = request.TableName;

        if (!TryParseDirection(request.Direction, out var direction))
            return BadRequest(new { message = "Invalid direction. Use Input/Output or 1/2." });

        if (!Enum.IsDefined(typeof(StatusCode), request.StatusCode))
            return BadRequest(new { message = "Invalid statusCode. Use 1 (Active) or 2 (Passive)." });

        table.Direction = direction;
        table.StatusCode = (StatusCode)request.StatusCode;

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // DELETE /api/decision-trees/{dtId}/tables/{id}
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int dtId,
        int id,
        CancellationToken ct)
    {
        var table = await _db.DecisionTreeTables
            .FirstOrDefaultAsync(x => x.Id == id && x.DecisionTreeId == dtId, ct);

        if (table == null)
            return NotFound(new { message = "Table not found" });

        _db.DecisionTreeTables.Remove(table);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private static bool TryParseDirection(JsonElement rawDirection, out TableDirection direction)
    {
        direction = default;

        if (rawDirection.ValueKind == JsonValueKind.Number)
        {
            if (!rawDirection.TryGetInt32(out var directionValue))
                return false;

            if (!Enum.IsDefined(typeof(TableDirection), directionValue))
                return false;

            direction = (TableDirection)directionValue;
            return true;
        }

        if (rawDirection.ValueKind == JsonValueKind.String)
        {
            var value = rawDirection.GetString();
            if (string.IsNullOrWhiteSpace(value))
                return false;

            if (int.TryParse(value, out var directionValue))
            {
                if (!Enum.IsDefined(typeof(TableDirection), directionValue))
                    return false;

                direction = (TableDirection)directionValue;
                return true;
            }

            return Enum.TryParse(value, ignoreCase: true, out direction);
        }

        return false;
    }
}
