using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GersoPersonalizados.Api.Data.Models;
using GersoPersonalizados.Api.Dtos;

namespace GersoPersonalizados.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly GersoDbContext _db;

    public OrdersController(GersoDbContext db)
    {
        _db = db;
    }


    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentDto dto)
    {
        if (dto is null) return BadRequest("Body is required.");
        if (dto.OrderId <= 0) return BadRequest("OrderId is required.");
        if (dto.Amount <= 0) return BadRequest("Amount must be greater than 0.");

        // (Opcional pero recomendado) Validar método permitido
        var allowedMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "CASH", "NEQUI", "DAVIPLATA", "TRANSFER"
    };

        var method = string.IsNullOrWhiteSpace(dto.Method) ? "CASH" : dto.Method.Trim().ToUpperInvariant();
        if (!allowedMethods.Contains(method))
            return BadRequest("Method must be one of: CASH, NEQUI, DAVIPLATA, TRANSFER.");

        // Traer la orden (para actualizar status si se paga completo)
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == dto.OrderId);
        if (order is null)
            return NotFound($"OrderId {dto.OrderId} not found.");

        // Traer resumen para saber balance actual
        var summaryBefore = await _db.vw_OrderSummary
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == dto.OrderId);

        if (summaryBefore is null)
            return BadRequest("Order summary not found. Ensure vw_OrderSummary exists.");

        if (dto.Amount > summaryBefore.Balance)
            return BadRequest($"Amount cannot exceed current balance ({summaryBefore.Balance}).");

        if (string.Equals(order.Status, "CLOSED", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Order is CLOSED. No more payments allowed.");

        // Crear pago
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

        // ---- AUTO-CLOSE (recalcula balance) ----
        var summaryAfter = await _db.vw_OrderSummary
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == dto.OrderId);

        if (summaryAfter != null)
        {
            if (summaryAfter.Balance <= 0.0001m)
            {
                // Solo cierra si no está cerrado ya
                if (!string.Equals(order.Status, "CLOSED", StringComparison.OrdinalIgnoreCase))
                {
                    order.Status = "CLOSED";
                    await _db.SaveChangesAsync();
                }
            }
        }

        // Devuelve el resumen actualizado para el frontend
        return Created($"/api/payments/{payment.PaymentId}", new
        {
            payment,
            orderSummary = summaryAfter
        });
    }

   

    [HttpPatch("{id:long}/close")]
    public async Task<IActionResult> CloseOrder(long id)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
        if (order is null) return NotFound();

        // Reglas simples: solo cerrar si está pagado
        var summary = await _db.vw_OrderSummary
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == id);

        if (summary is null)
            return BadRequest("Order summary not found.");

        if (summary.Balance > 0)
            return BadRequest($"Cannot close order. Balance pending: {summary.Balance}");

        order.Status = "CLOSED";
        await _db.SaveChangesAsync();

        // Devuelve resumen actualizado
        var updated = await _db.vw_OrderSummary
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == id);

        if (updated == null)
        {
            return Ok(new
            {
                orderId = id,
                status = "CLOSED"
            });
        }

        return Ok(updated);


    }


}