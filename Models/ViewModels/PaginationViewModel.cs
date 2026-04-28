namespace Bevera.Models.ViewModels
{
    public class PaginationViewModel
    {
        public int Page { get; set; } = 1;

        public int TotalPages { get; set; } = 1;

        public string Controller { get; set; } = "";

        public string Action { get; set; } = "";

        public Dictionary<string, string?> RouteValues { get; set; } = new();
    }
}