using System;
using System.Collections.Generic;

namespace GersoPersonalizados.Api.Data.Models;

public partial class OrderItems
{
    public long OrderItemId { get; set; }

    public long OrderId { get; set; }

    public int? ProductId { get; set; }

    public string Description { get; set; } = null!;

    public int Qty { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal? LineTotal { get; set; }

    public string? Notes { get; set; }

    public virtual Orders Order { get; set; } = null!;

    public virtual Products? Product { get; set; }
}
