using System;
using System.Collections.Generic;

namespace GersoPersonalizados.Api.Data.Models;

public partial class vw_OrderSummary
{
    public long OrderId { get; set; }

    public int CustomerId { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string Status { get; set; } = null!;

    public string DeliveryType { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal? Balance { get; set; }
}
