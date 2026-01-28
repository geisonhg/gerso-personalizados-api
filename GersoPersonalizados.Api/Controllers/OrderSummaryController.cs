using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GersoPersonalizados.Api.Data.Models;

namespace GersoPersonalizados.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrderSummaryController : ControllerBase
{
    private readonly GersoDbContext _db;

    public OrderSummaryController(GersoDbContext db)
    {
        _db = db;
    }

    // GET: /api/orders/summary?phone=3001234567&status=NEW&from=2026-01-01&to=2026-01-31&openOnly=true
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string? phone,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool openOnly = false)
    {
        // Ajusta el nombre del DbSet si en tu DbContext se llama distinto:
        // puede ser VwOrderSummary o vw_OrderSummary dependiendo del scaffold
        var query = _db.vw_OrderSummary.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(phone))
            query = query.Where(x => x.Phone == phone);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);

        if (from.HasValue)
            query = query.Where(x => x.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.CreatedAt <= to.Value);

        if (openOnly)
            query = query.Where(x => x.Balance > 0);

        var result = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(200) // límite seguro para swagger
            .ToListAsync();

        return Ok(result);
    }
}
