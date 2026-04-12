using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Định dạng email không hợp lệ")]
        [Display(Name = "Email của bạn")]
        public string Email { get; set; } = string.Empty;
    }
}
