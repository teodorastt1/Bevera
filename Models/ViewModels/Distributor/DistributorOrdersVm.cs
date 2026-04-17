namespace Bevera.Models.ViewModels.Distributor
{
    public class DistributorOrdersVm
    {
        public int DistributorId { get; set; }
        public string DistributorName { get; set; } = "";
        public bool IsPreviewMode { get; set; }
        public string ActiveTab { get; set; } = "preparing";
        public List<DistributorOrderListRowVm> Orders { get; set; } = new();
    }

    public class DistributorOrderListRowVm
    {
        public int Id { get; set; }
        public string Status { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReceivedAt { get; set; }
        public int ItemsCount { get; set; }
    }
}
