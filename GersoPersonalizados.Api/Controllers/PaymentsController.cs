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
        if (dto is null) return BadRequest("Body is required.");
        if (dto.OrderId <= 0) return BadRequest("OrderId is required.");
        if (dto.Amount <= 0) return BadRequest("Amount must be greater than 0.");

        var allowedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "CASH", "NEQUI", "DAVIPLATA", "TRANSFER" };

        var method = string.IsNullOrWhiteSpace(dto.Method)
            ? "CASH"
            : dto.Method.Trim().ToUpperInvariant();

        if (!allowedMethods.Contains(method))
            return BadRequest("Method must be one of: CASH, NEQUI, DAVIPLATA, TRANSFER.");

        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == dto.OrderId);
        if (order is null) return NotFound($"OrderId {dto.OrderId} not found.");

        if (string.Equals(order.Status, "CLOSED", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Order is CLOSED. No more payments allowed.");

        var summaryBefore = await _db.vw_OrderSummary
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == dto.OrderId);

        if (summaryBefore is null)
            return BadRequest("Order summary not found. Ensure vw_OrderSummary exists.");

        if (dto.Amount > summaryBefore.Balance)
            return BadRequest($"Amount cannot exceed current balance ({summaryBefore.Balance}).");

        var payment = new Payments
        {
            OrderId = dto.OrderId,
            Amount = dto.Amount,
            Method = method,
            Reference = dto.Reference?.Trim(),
            Notes = dto.Notes?.Trim(),
            PaidAt = dto.PaidAt ?? DateTime.UtcNow
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        // ✅ ESTA ES LA PIEZA QUE TE FALTA
        var summaryAfter = await _db.vw_OrderSummary
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == dto.OrderId);

        // Auto-close si quedó en 0
        if (summaryAfter != null && summaryAfter.Balance <= 0.0001m &&
            !string.Equals(order.Status, "CLOSED", StringComparison.OrdinalIgnoreCase))
        {
            order.Status = "CLOSED";
            await _db.SaveChangesAsync();

            // opcional: recargar summary por si tu vista depende del status
            summaryAfter = await _db.vw_OrderSummary
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.OrderId == dto.OrderId);
        }

        return Created($"/api/payments/{payment.PaymentId}", new
        {
            payment = new
            {
                payment.PaymentId,
                payment.OrderId,
                payment.Amount,
                payment.Method,
                payment.Reference,
                payment.Notes,
                payment.PaidAt
            },
            orderSummary = summaryAfter
        });
    }



    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var payment = await _db.Payments.FindAsync(id);
        return payment is null ? NotFound() : Ok(payment);
    }
}
