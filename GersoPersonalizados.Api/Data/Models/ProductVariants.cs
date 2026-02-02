namespace GersoPersonalizados.Api.Data.Models
{
    public partial class ProductVariants
    {
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual Products Product { get; set; } = null!;
    }
}
