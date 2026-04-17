namespace Bevera.Models.ViewModels.Distributor
{
    public class DistributorDashboardVm
    {
        public int DistributorId { get; set; }
        public string DistributorName { get; set; } = "";
        public bool IsPreviewMode { get; set; }
        public int NewOrdersCount { get; set; }
        public int PreparingOrdersCount { get; set; }
        public int CompletedOrdersCount { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
