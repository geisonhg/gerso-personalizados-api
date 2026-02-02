using System;
using System.Collections.Generic;

namespace GersoPersonalizados.Api.Data.Models;

public class Products
{
    public int ProductId { get; set; }

    public string Name { get; set; } = "";

    public decimal BasePrice { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public ICollection<OrderItems> OrderItems { get; set; } = new List<OrderItems>();
}