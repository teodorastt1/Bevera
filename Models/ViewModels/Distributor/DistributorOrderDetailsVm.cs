using System;
using System.Collections.Generic;

namespace Bevera.Models.ViewModels.Distributor
{
    public class DistributorOrderDetailsVm
    {
        public int DistributorId { get; set; }
        public string DistributorName { get; set; } = "";
        public int OrderId { get; set; }
        public string Status { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public string? Notes { get; set; }
        public bool IsPreviewMode { get; set; }

        public List<DistributorOrderDetailsItemVm> Items { get; set; } = new();
    }

    public class DistributorOrderDetailsItemVm
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }

        // Цена за 1 брой
        public decimal CostPrice { get; set; }

        // Общо за реда
        public decimal LineTotal { get; set; }

        public int UnitsPerCase { get; set; } = 12;

        public int CasesCount =>
            UnitsPerCase > 0
                ? (int)Math.Ceiling((decimal)Quantity / UnitsPerCase)
                : Quantity;

        public string DisplayQuantity =>
            UnitsPerCase > 0
                ? $"{CasesCount} каш. ({Quantity} бр.)"
                : $"{Quantity} бр.";
    }

    public class DistributorOrderPriceInputVm
    {
        public int Id { get; set; }

        // Пази се като текст, за да приемаме и 3,6 и 3.6
        public string UnitPrice { get; set; } = "";
    }
}