namespace GersoPersonalizados.Api.Dtos;

public class CreateOrderItemDto
{
    // Si viene del catálogo
    public int? ProductId { get; set; }
    public int? VariantId { get; set; }

    // Siempre requerido (incluso si viene del catálogo, lo puedes autocompletar)
    public string Description { get; set; } = "";

    public int Qty { get; set; } = 1;

    // Para manual, viene del front. Para catálogo, lo calculamos con BasePrice
    public decimal UnitPrice { get; set; }

    // extra por diseño complicado, urgencia, etc.
    public decimal ExtraAmount { get; set; } = 0m;

    public string? Notes { get; set; }
}