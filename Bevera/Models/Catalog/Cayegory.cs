using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.Catalog
{
    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; } = "";

        // път до снимката в wwwroot (пример: /uploads/categories/coffee.jpg)
        [StringLength(300)]
        public string? ImagePath { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
