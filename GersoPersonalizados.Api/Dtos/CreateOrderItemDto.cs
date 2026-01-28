namespace GersoPersonalizados.Api.Dtos;

public class CreateOrderItemDto
{
    public string Description { get; set; } = "";
    public int Qty { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}
