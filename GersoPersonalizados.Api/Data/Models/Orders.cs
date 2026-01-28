using System;
using System.Collections.Generic;

namespace GersoPersonalizados.Api.Data.Models;

public partial class Orders
{
    public long OrderId { get; set; }

    public int CustomerId { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public string DeliveryType { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public int? CreatedByUserId { get; set; }

    public virtual Users? CreatedByUser { get; set; }

    public virtual Customers Customer { get; set; } = null!;

    public virtual ICollection<OrderItems> OrderItems { get; set; } = new List<OrderItems>();

    public virtual ICollection<Payments> Payments { get; set; } = new List<Payments>();
}
