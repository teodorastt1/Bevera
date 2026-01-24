using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(60)]
        public string Name { get; set; } = "";

        [Required, StringLength(80)]
        public string Slug { get; set; } = ""; // beer, water...

        public bool IsActive { get; set; } = true;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
