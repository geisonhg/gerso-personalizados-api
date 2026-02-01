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

        var allowedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CASH",
        "NEQUI",
        "DAVIPLATA",
        "TRANSFER"
    };

        var method = string.IsNullOrWhiteSpace(dto.Method)
            ? "CASH"
            : dto.Method.Trim().ToUpperInvariant();

        if (!allowedMethods.Contains(method))
            return BadRequest("Method must be one of: CASH, NEQUI, DAVIPLATA, TRANSFER.");


        if (dto is null) return BadRequest("Body is required.");

        if (dto.OrderId <= 0)
            return BadRequest("OrderId is required.");

        if (dto.Amount <= 0)
            return BadRequest("Amount must be greater than 0.");

        // Verifica que exista el pedido
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == dto.OrderId);
        if (order is null)
            return NotFound($"OrderId {dto.OrderId} not found.");

        // Trae el resumen para validar balance
        var summary = await _db.vw_OrderSummary
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == dto.OrderId);

        if (summary is null)
            return BadRequest("Order summary not found. Ensure vw_OrderSummary exists.");

        if (dto.Amount > summary.Balance)
            return BadRequest($"Amount cannot exceed current balance ({summary.Balance}).");

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

        // Devuelve el resumen actualizado (para UI)
        var updated = await _db.vw_OrderSummary
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == dto.OrderId);

        // Si quedó pagado (balance <= 0), cerramos automáticamente
        if (updated is not null && updated.Balance <= 0)
        {
            
            var orderToClose = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == dto.OrderId);
            if (orderToClose is not null && orderToClose.Status != "PAID")
            {
                orderToClose.Status = "PAID"; // o "DONE" si prefieres
                await _db.SaveChangesAsync();

                // refresca updated por si la view incluye status
                updated = await _db.vw_OrderSummary.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.OrderId == dto.OrderId);
            }
        }

        return Created($"/api/payments/{payment.PaymentId}", new
        {
            payment,
            orderSummary = updated
        });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var payment = await _db.Payments.FindAsync(id);
        return payment is null ? NotFound() : Ok(payment);
    }
}
