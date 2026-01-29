using DecisionTree.Api.Contracts.DecisionTrees;
using DecisionTree.Api.Data;
using DecisionTree.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                x.TableCode,
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
                x.TableCode,
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
        if (request.DecisionTreeId != dtId)
            return BadRequest(new { message = "DecisionTreeId mismatch" });

        var dtExists = await _db.DecisionTrees.AnyAsync(x => x.Id == dtId, ct);
        if (!dtExists)
            return NotFound(new { message = "Decision tree not found" });

        // Check for duplicate TableCode within same DecisionTree
        var codeExists = await _db.DecisionTreeTables
            .AnyAsync(x => x.DecisionTreeId == dtId && x.TableCode == request.TableCode, ct);

        if (codeExists)
            return BadRequest(new { message = $"Table code '{request.TableCode}' already exists" });

        var table = new DecisionTreeTable
        {
            DecisionTreeId = request.DecisionTreeId,
            TableCode = request.TableCode,
            TableName = request.TableName,
            Direction = Enum.Parse<TableDirection>(request.Direction),
            StatusCode = (StatusCode)request.StatusCode
        };

        _db.DecisionTreeTables.Add(table);
        await _db.SaveChangesAsync(ct);

        var dto = new DecisionTreeTableDto(
            table.Id,
            table.DecisionTreeId,
            table.TableCode,
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

        // Check for duplicate TableCode (excluding current table)
        var codeExists = await _db.DecisionTreeTables
            .AnyAsync(x => x.DecisionTreeId == dtId && x.TableCode == request.TableCode && x.Id != id, ct);

        if (codeExists)
            return BadRequest(new { message = $"Table code '{request.TableCode}' already exists" });

        table.TableCode = request.TableCode;
        table.TableName = request.TableName;
        table.Direction = Enum.Parse<TableDirection>(request.Direction);
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
}
