using System;
using System.Collections.Generic;

namespace GersoPersonalizados.Api.Data.Models;

public partial class Products
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public decimal? BasePrice { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<OrderItems> OrderItems { get; set; } = new List<OrderItems>();
}
