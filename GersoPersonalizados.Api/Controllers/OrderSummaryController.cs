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
    // GET /api/orders/summary?q=ana&openOnly=true&take=200
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] string? q,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool openOnly = false,
        [FromQuery] int take = 200)
    {
        if (take is < 1 or > 500) take = 200;

        var query = _db.vw_OrderSummary.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.Phone.Contains(term) || x.FullName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim().ToUpperInvariant();
            query = query.Where(x => x.Status == s);
        }

        if (from.HasValue)
            query = query.Where(x => x.CreatedAt >= from.Value);

        if (to.HasValue)
        {
            var end = to.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(x => x.CreatedAt <= end);
        }

        if (openOnly)
            query = query.Where(x => x.Balance > 0);

        var result = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync();

        return Ok(result);
    }
}
