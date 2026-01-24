using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models
{
    public class Brand
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; } = "";

        public bool IsActive { get; set; } = true;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
