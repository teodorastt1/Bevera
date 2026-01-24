using System.ComponentModel.DataAnnotations;

namespace Bevera.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        [Required, StringLength(255)]
        public string Url { get; set; } = ""; // "/images/products/cola-1.jpg"

        public bool IsMain { get; set; } = false;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }
}
