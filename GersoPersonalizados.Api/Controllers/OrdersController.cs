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

    //  POST /api/orders  -> Create Order
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        if (dto is null) return BadRequest("Body is required.");

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest("FullName is required.");

        if (string.IsNullOrWhiteSpace(dto.Phone))
            return BadRequest("Phone is required.");

        if (dto.Items is null || dto.Items.Count == 0)
            return BadRequest("At least 1 item is required.");

        foreach (var it in dto.Items)
        {
            if (string.IsNullOrWhiteSpace(it.Description))
                return BadRequest("Each item must have Description.");

            if (it.Qty <= 0)
                return BadRequest("Each item must have Qty > 0.");

            if (it.UnitPrice <= 0)
                return BadRequest("Each item must have UnitPrice > 0.");
        }

        // Normaliza (WhatsApp-first)
        var phone = dto.Phone.Trim();
        var fullName = dto.FullName.Trim();
        var deliveryType = string.IsNullOrWhiteSpace(dto.DeliveryType)
            ? "PICKUP"
            : dto.DeliveryType.Trim().ToUpperInvariant();

        await using var tx = await _db.Database.BeginTransactionAsync();

        // 1) Cliente por teléfono
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Phone == phone);

        if (customer is null)
        {
            customer = new Customers
            {
                FullName = fullName,
                Phone = phone,
                Notes = null,
                CreatedAt = DateTime.UtcNow
            };
            _db.Customers.Add(customer);
            await _db.SaveChangesAsync();
        }
        else
        {
            // opcional: actualiza nombre si cambió
            if (!string.Equals(customer.FullName, fullName, StringComparison.Ordinal))
                customer.FullName = fullName;
        }

        // 2) Orden
        var order = new Orders
        {
            CustomerId = customer.CustomerId,
            CreatedAt = DateTime.UtcNow,
            Status = "NEW",
            DeliveryType = deliveryType,
            Notes = dto.Notes,
            TotalAmount = 0m
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(); // para OrderId

        // 3) Items + total
        decimal total = 0m;

        foreach (var it in dto.Items)
        {
            var extra = it.ExtraAmount; // puede ser 0
            var lineTotal = (it.UnitPrice * it.Qty) + extra;
            total += lineTotal;

            var item = new OrderItems
            {
                OrderId = order.OrderId,

                // catálogo u "otros"
                ProductId = it.ProductId,     // puede ser null
                VariantId = it.VariantId,     // puede ser null

                Description = it.Description.Trim(),
                Qty = it.Qty,
                UnitPrice = it.UnitPrice,
                ExtraAmount = extra,        
                LineTotal = lineTotal,
                Notes = it.Notes
            };

            _db.OrderItems.Add(item);
        }

        order.TotalAmount = total;

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        // 4) Respuesta: summary de la VIEW
        var summary = await _db.vw_OrderSummary
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == order.OrderId);

        if (summary is null)
        {
            return Created($"/api/orders/{order.OrderId}", new
            {
                orderId = order.OrderId,
                customerId = customer.CustomerId,
                totalAmount = order.TotalAmount,
                status = order.Status
            });
        }

        return Created($"/api/orders/{order.OrderId}", summary);
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

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetOrderById(long id)
    {
        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Customer)
            .Include(o => o.OrderItems)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order is null) return NotFound();

        // Summary (de la view)
        var summary = await _db.vw_OrderSummary
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == id);

        return Ok(new
        {
            order.OrderId,
            order.CreatedAt,
            order.Status,
            order.DeliveryType,
            order.TotalAmount,
            order.Notes,
            customer = new
            {
                order.Customer.CustomerId,
                order.Customer.FullName,
                order.Customer.Phone
            },
            items = order.OrderItems.Select(i => new
            {
                i.OrderItemId,
                i.Description,
                i.Qty,
                i.UnitPrice,
                i.LineTotal,
                i.Notes
            }),
            payments = order.Payments
                .OrderByDescending(p => p.PaidAt)
                .Select(p => new
                {
                    p.PaymentId,
                    p.PaidAt,
                    p.Amount,
                    p.Method,
                    p.Reference,
                    p.Notes
                }),
            summary
        });
    }

    [HttpPatch("{id:long}")]
    public async Task<IActionResult> UpdateOrder(long id, [FromBody] UpdateOrderDto dto)
    {
        if (dto is null) return BadRequest("Body is required.");

        var order = await _db.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderId == id);

        if (order is null) return NotFound();

        if (string.Equals(order.Status, "CLOSED", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Order is CLOSED. No changes allowed.");

        // DeliveryType / Notes
        if (!string.IsNullOrWhiteSpace(dto.DeliveryType))
            order.DeliveryType = dto.DeliveryType.Trim().ToUpperInvariant();

        if (dto.Notes != null) // permite limpiar notas mandando ""
            order.Notes = dto.Notes;

        // Items (reemplazo total)
        if (dto.Items is not null)
        {
            if (dto.Items.Count == 0)
                return BadRequest("Items cannot be empty (send null if you don't want to change items).");

            foreach (var it in dto.Items)
            {
                if (string.IsNullOrWhiteSpace(it.Description))
                    return BadRequest("Each item must have Description.");
                if (it.Qty <= 0)
                    return BadRequest("Each item must have Qty > 0.");
                if (it.UnitPrice <= 0)
                    return BadRequest("Each item must have UnitPrice > 0.");
            }

            // borrar items actuales
            _db.OrderItems.RemoveRange(order.OrderItems);

            decimal total = 0m;

            foreach (var it in dto.Items)
            {
                var lineTotal = it.UnitPrice * it.Qty;
                total += lineTotal;

                _db.OrderItems.Add(new OrderItems
                {
                    OrderId = order.OrderId,
                    ProductId = null,
                    Description = it.Description.Trim(),
                    Qty = it.Qty,
                    UnitPrice = it.UnitPrice,
                    LineTotal = lineTotal,
                    Notes = it.Notes
                });
            }

            order.TotalAmount = total;
        }

        await _db.SaveChangesAsync();

        // devolver detalle actualizado (reusa tu GetOrderById)
        return await GetOrderById(id);
    }


}