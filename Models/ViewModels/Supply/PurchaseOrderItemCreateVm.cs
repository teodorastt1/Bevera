using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models.ViewModels.Supply
{
    public class PurchaseOrderItemCreateVm
    {
        public int PurchaseOrderId { get; set; }

        // compatibility for AddDistributorProduct
        [Display(Name = "Продукт")]
        public int ProductId { get; set; }

        [Range(0, 999999, ErrorMessage = "Невалидна цена.")]
        [Display(Name = "Доставна цена от дистрибутора")]
        public decimal CostPrice { get; set; }

        public List<PurchaseOrderItemBulkRowVm> Items { get; set; } = new();

        public List<SelectListItem> Products { get; set; } = new();
    }

    public class PurchaseOrderItemBulkRowVm
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = "";

        public bool IsSelected { get; set; }

        [Range(1, 999999, ErrorMessage = "Броят кашони трябва да е поне 1.")]
        [Display(Name = "Кашони")]
        public int CasesCount { get; set; } = 1;

        [Range(1, 120, ErrorMessage = "Броят в кашон трябва да е между 1 и 120.")]
        [Display(Name = "Бройки в кашон")]
        public int UnitsPerCase { get; set; } = 12;

        public int RequestedUnits => CasesCount * UnitsPerCase;

        public decimal CostPrice { get; set; }

        public bool HasDistributorPrice { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}
