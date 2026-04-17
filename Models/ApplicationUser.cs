using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Bevera.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(60)]
        public string FirstName { get; set; } = "";

        [StringLength(60)]
        public string LastName { get; set; } = "";

        [StringLength(200)]
        public string Address { get; set; } = "";

        public DateTime CreatedAt { get; set; }
    }
}
