using System;
using System.ComponentModel.DataAnnotations;

namespace Websitebanhang.Models.ViewModels
{
    public class ProfileViewModel
    {
        public string? Email { get; set; }

        [Required(ErrorMessage = "Tên hiển thị không được để trống")]
        [Display(Name = "Tên hiển thị")]
        public string? UserName { get; set; }

        [Required(ErrorMessage = "Họ và tên không được để trống")]
        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [Display(Name = "Số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }
    }
}
