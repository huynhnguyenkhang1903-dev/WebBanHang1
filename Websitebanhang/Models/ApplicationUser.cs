using Microsoft.AspNetCore.Identity;

namespace Websitebanhang.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public DateTime? DateOfBirth { get; set; }

        public string AvatarUrl { get; set; } = string.Empty;
    }
}