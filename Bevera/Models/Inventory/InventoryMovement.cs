using System;
using System.ComponentModel.DataAnnotations;
using Bevera.Models.Catalog;

namespace Bevera.Models.Inventory
{
    public class InventoryMovement
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // + за зареждане, - за изписване
        public int QuantityChange { get; set; }

        [StringLength(250)]
        public string? Reason { get; set; }  // напр. "Restock", "Order #123"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // кой го е направил (Admin/Worker)
        [Required]
        public string PerformedByUserId { get; set; } = "";
    }
}
