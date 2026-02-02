namespace GersoPersonalizados.Api.Dtos;

public class UpdateOrderDto
{
    public string? DeliveryType { get; set; } // PICKUP / DELIVERY
    public string? Notes { get; set; }

    public List<UpdateOrderItemDto>? Items { get; set; }
}