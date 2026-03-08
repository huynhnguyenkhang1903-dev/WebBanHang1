using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Websitebanhang.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string? Email { get; set; }
        }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Ở đây bạn có thể thêm logic gửi email reset password
            TempData["Message"] = "Nếu email tồn tại, link đặt lại mật khẩu sẽ được gửi.";

            return RedirectToPage("Login");
        }
    }
}