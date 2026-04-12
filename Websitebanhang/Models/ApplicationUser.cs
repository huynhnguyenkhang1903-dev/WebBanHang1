using Microsoft.AspNetCore.Identity;

namespace Websitebanhang.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        public string? Address { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public string? AvatarUrl { get; set; }
    }
}