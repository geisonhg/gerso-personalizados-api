using System;
using System.Collections.Generic;

namespace GersoPersonalizados.Api.Data.Models;

public partial class Payments
{
    public long PaymentId { get; set; }

    public long OrderId { get; set; }

    public DateTime PaidAt { get; set; }

    public decimal Amount { get; set; }

    public string Method { get; set; } = null!;

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public virtual Orders Order { get; set; } = null!;
}
