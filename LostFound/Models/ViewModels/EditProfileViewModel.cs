using System.ComponentModel.DataAnnotations;

namespace LostFound.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        public IFormFile? AvatarFile { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string? CurrentPassword { get; set; }

        [DataType(DataType.Password), StringLength(100, MinimumLength = 6)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password), Compare(nameof(NewPassword))]
        [Display(Name = "Confirm New Password")]
        public string? ConfirmNewPassword { get; set; }
    }
}
