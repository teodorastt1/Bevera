namespace Bevera.Models.ViewModels.Distributor
{
    public class DistributorProductsVm
    {
        public int DistributorId { get; set; }
        public string DistributorName { get; set; } = "";
        public bool IsPreviewMode { get; set; }
        public List<DistributorProductManageVm> Products { get; set; } = new();
    }

    public class DistributorProductManageVm
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal CostPrice { get; set; }
        public bool IsAvailable { get; set; }
        public string? CategoryName { get; set; }
    }
}
