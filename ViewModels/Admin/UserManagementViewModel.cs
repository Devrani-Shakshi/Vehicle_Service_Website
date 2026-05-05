using System.ComponentModel.DataAnnotations;

namespace ServicePlatform.ViewModels.Admin;

public class UserCreateViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 100 characters.")]
    public string FullName { get; set; } = string.Empty;

    [Required, MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "User";

    [Required]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Please enter a valid 10-digit Indian mobile number starting with 6-9.")]
    public string? Mobile { get; set; }

    [Required, MaxLength(100)]
    public string? State { get; set; }
}

public class UserEditViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    public string? Role { get; set; }

    [Required]
    [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Please enter a valid 10-digit Indian mobile number starting with 6-9.")]
    public string? Mobile { get; set; }

    [MaxLength(100)]
    public string? State { get; set; }

    public bool IsActive { get; set; }
}

public class FeedbackViewModel
{
    [Required(ErrorMessage = "Message is required")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Feedback must be between 10 and 2000 characters")]
    public string Message { get; set; } = string.Empty;

    [Required, Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }
}
