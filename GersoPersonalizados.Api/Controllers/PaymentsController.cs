using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GersoPersonalizados.Api.Data.Models;
using GersoPersonalizados.Api.Dtos;

namespace GersoPersonalizados.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly GersoDbContext _db;

    public PaymentsController(GersoDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest("Amount must be greater than 0.");

        var orderExists = await _db.Orders.AnyAsync(o => o.OrderId == dto.OrderId);
        if (!orderExists)
            return NotFound($"OrderId {dto.OrderId} not found.");

        var payment = new Payments
        {
            OrderId = dto.OrderId,
            Amount = dto.Amount,
            Method = dto.Method,
            Reference = dto.Reference,
            Notes = dto.Notes,
            PaidAt = dto.PaidAt ?? DateTime.Now
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = payment.PaymentId }, payment);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var payment = await _db.Payments.FindAsync(id);
        return payment is null ? NotFound() : Ok(payment);
    }
}
