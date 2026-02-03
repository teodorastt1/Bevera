using System.Collections.Generic;

namespace Bevera.Models.ViewModels
{
    public class ClientProfileViewModel
    {
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }

        public List<ClientOrderRowViewModel> Orders { get; set; } = new();
    }

    public class ClientOrderRowViewModel
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = "";
        public decimal Total { get; set; }
        public string CreatedAt { get; set; } = "";
    }
}
