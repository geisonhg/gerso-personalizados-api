using System;
using System.Collections.Generic;

namespace GersoPersonalizados.Api.Data.Models;

public partial class Customers
{
    public int CustomerId { get; set; }

    public string FullName { get; set; } = null!;

    public string Phone { get; set; } = null!;

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Orders> Orders { get; set; } = new List<Orders>();
}
