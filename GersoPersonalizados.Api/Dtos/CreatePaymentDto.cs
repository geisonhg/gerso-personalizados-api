namespace GersoPersonalizados.Api.Dtos;

public class CreatePaymentDto
{
    public long OrderId { get; set; }          
    public decimal Amount { get; set; }

    public string Method { get; set; } = "CASH";   // NEQUI, DAVIPLATA, TRANSFER, CASH
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime? PaidAt { get; set; }
}