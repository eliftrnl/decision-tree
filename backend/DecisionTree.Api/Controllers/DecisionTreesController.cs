using DecisionTree.Api.Contracts.DecisionTrees;
using DecisionTree.Api.Data;
using DecisionTree.Api.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using DecisionTreeEntity = DecisionTree.Api.Entities.DecisionTree;
using DtStatusCode = DecisionTree.Api.Entities.StatusCode;

namespace DecisionTree.Api.Controllers;

[ApiController]
[Route("api/decision-trees")]
public class DecisionTreesController : ControllerBase
{
    private readonly AppDbContext _db;

    public DecisionTreesController(AppDbContext db)
    {
        _db = db;
    }

    // GET /api/DecisionTrees?code=&name=&status=
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DecisionTreeListItemDto>>> GetAll(
        [FromQuery] string? code,
        [FromQuery] string? name,
        [FromQuery] DtStatusCode? status,
        CancellationToken ct)
    {
        var q = _db.DecisionTrees
            .AsNoTracking()
            .AsQueryable();

        // Code: exact match
        if (!string.IsNullOrWhiteSpace(code))
        {
            var c = code.Trim();
            q = q.Where(x => x.Code == c);
        }

        // Name: contains (case-insensitive)
        if (!string.IsNullOrWhiteSpace(name))
        {
            var n = name.Trim();
            q = q.Where(x => x.Name.Contains(n));
        }

        if (status.HasValue)
            q = q.Where(x => x.StatusCode == status.Value);

        var list = await q
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new DecisionTreeListItemDto(
                x.Id,
                x.Code,
                x.Name,
                (int)x.StatusCode,
                x.UpdatedAtUtc // UI "Last Operation Date" için
            ))
            .ToListAsync(ct);

        return Ok(list);
    }

    // GET /api/DecisionTrees/exists?code=xxx
    [HttpGet("exists")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> Exists([FromQuery] string code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Ok(new { exists = false });

        var c = code.Trim();

        var exists = await _db.DecisionTrees
            .AsNoTracking()
            .AnyAsync(x => x.Code == c, ct);

        return Ok(new { exists });
    }

    // GET /api/DecisionTrees/5
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DecisionTreeEntity>> GetById([FromRoute] int id, CancellationToken ct)
    {
        var item = await _db.DecisionTrees
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return item is null ? NotFound() : Ok(item);
    }

    // POST /api/DecisionTrees
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DecisionTreeEntity>> Create(
        [FromBody] DecisionTreeCreateRequest req,
        CancellationToken ct)
    {
        if (req is null)
            return BadRequest("Body boş olamaz.");

        var code = req.Code?.Trim();
        var name = req.Name?.Trim();

        if (string.IsNullOrWhiteSpace(code))
            return BadRequest("Code boş olamaz.");

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name boş olamaz.");

        var entity = new DecisionTreeEntity
        {
            Code = code,
            Name = name,
            StatusCode = DtStatusCode.Active, // enum'unda Active yoksa uygun değeri seç
            SchemaVersion = 1,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = null
        };

        _db.DecisionTrees.Add(entity);

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is MySqlException { Number: 1062 })
        {
            return Conflict("Bu Code zaten mevcut.");
        }

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, entity);
    }

    // PUT /api/DecisionTrees/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] DecisionTreeUpdateRequest req,
        CancellationToken ct)
    {
        var entity = await _db.DecisionTrees.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null) return NotFound();

        var name = req.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Name boş olamaz.");

        entity.Name = name;
        entity.StatusCode = (DtStatusCode)req.StatusCode;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE /api/DecisionTrees/5
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken ct)
    {
        var item = await _db.DecisionTrees.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return NotFound();

        _db.DecisionTrees.Remove(item);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
