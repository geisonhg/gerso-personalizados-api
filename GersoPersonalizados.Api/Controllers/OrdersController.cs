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
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest("FullName is required.");

        if (string.IsNullOrWhiteSpace(dto.Phone))
            return BadRequest("Phone is required.");

        if (dto.Items == null || dto.Items.Count == 0)
            return BadRequest("At least 1 item is required.");

        if (dto.Items.Any(i => i.Qty <= 0 || i.UnitPrice < 0 || string.IsNullOrWhiteSpace(i.Description)))
            return BadRequest("Each item must have Description, Qty > 0 and UnitPrice >= 0.");

        // 1) Buscar cliente por teléfono (tu sistema es WhatsApp-first)
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Phone == dto.Phone);

        // 2) Si no existe, crearlo
        if (customer == null)
        {
            customer = new Customers
            {
                FullName = dto.FullName,
                Phone = dto.Phone,
                Notes = null,
                CreatedAt = DateTime.Now
            };

            _db.Customers.Add(customer);
            await _db.SaveChangesAsync(); // para obtener CustomerId
        }

        // 3) Crear el pedido
        var order = new Orders
        {
            CustomerId = customer.CustomerId,
            CreatedAt = DateTime.Now,
            Status = "NEW",
            DeliveryType = dto.DeliveryType ?? "PICKUP",
            TotalAmount = 0m
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(); // para obtener OrderId

        // 4) Crear items y calcular total
        decimal total = 0m;

        foreach (var item in dto.Items)
        {
            var lineTotal = item.Qty * item.UnitPrice;
            total += lineTotal;

            var orderItem = new OrderItems
            {
                OrderId = order.OrderId,
                ProductId = null,
                Description = item.Description,
                Qty = item.Qty,
                UnitPrice = item.UnitPrice,
                LineTotal = lineTotal,
                Notes = item.Notes
            };

            _db.OrderItems.Add(orderItem);
        }

        // 5) Guardar total en la orden
        order.TotalAmount = total;

        await _db.SaveChangesAsync();

        // 6) Respuesta útil: OrderId + link de consulta
        return Ok(new
        {
            orderId = order.OrderId,
            customerId = customer.CustomerId,
            totalAmount = total,
            message = "Order created.",
            summaryUrl = $"/api/orders/summary?phone={customer.Phone}"
        });
    }
}
