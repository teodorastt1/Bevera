using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, StringLength(140)]
        public string Name { get; set; } = ""; // "Coca-Cola"

        [StringLength(80)]
        public string? SKU { get; set; } // по желание

        [Range(0, 999999)]
        public decimal Price { get; set; }

        // drink-specific
        [Range(0, 10)]
        public decimal VolumeLiters { get; set; } // 0.5, 1.5...

        [Range(0, 100)]
        public decimal AlcoholPercent { get; set; } // 0 за безалкохолно

        [StringLength(40)]
        public string? PackageType { get; set; } // "Bottle", "Can"

        public bool IsActive { get; set; } = true;

        // relations
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public int? BrandId { get; set; }
        public Brand? Brand { get; set; }

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    }
}
