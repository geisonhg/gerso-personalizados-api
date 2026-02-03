namespace GersoPersonalizados.Api.Data.Models
{
    public partial class ProductVariants
    {
        public int VariantId { get; set; }     // ✅ PK

        public int ProductId { get; set; }     // FK a Products
        public string Name { get; set; } = "";
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // navs (opcional, pero recomendado)
        public virtual Products? Product { get; set; }
        public virtual ICollection<OrderItems> OrderItems { get; set; } = new List<OrderItems>();
    }
}
