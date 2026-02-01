using GersoPersonalizados.Api.Dtos;

public class CreateOrderDto
{
    public string FullName { get; set; } = "";
    public string Phone { get; set; } = "";
    public string? DeliveryType { get; set; } = "PICKUP"; // PICKUP / DELIVERY
    public string? Notes { get; set; }

    public string? Status { get; set; } = "NEW"; // opcional

    public List<CreateOrderItemDto> Items { get; set; } = new();
}