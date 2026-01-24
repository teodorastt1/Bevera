using Bevera.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string ClientId { get; set; } = "";
        public ApplicationUser Client { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required, StringLength(20)]
        public string Status { get; set; } = "Pending";

        [Required, StringLength(20)]
        public string PaymentStatus { get; set; } = "Unpaid";

        [Range(0, 99999999)]
        public decimal Total { get; set; }

        // snapshot contact/delivery
        [Required, StringLength(120)]
        public string FullName { get; set; } = "";

        [Required, StringLength(120)]
        public string Email { get; set; } = "";

        [StringLength(30)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Address { get; set; }

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public ICollection<OrderStatusHistory> History { get; set; } = new List<OrderStatusHistory>();
       
        public DateTime? UpdatedAt { get; set; }

        public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    }

}
