namespace GersoPersonalizados.Api.Dtos;

public class UpdateOrderItemDto
{
    public string Description { get; set; } = "";
    public int Qty { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}