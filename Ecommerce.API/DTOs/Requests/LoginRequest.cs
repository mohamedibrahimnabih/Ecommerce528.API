using System.ComponentModel.DataAnnotations;

namespace Ecommerce.API.DTOs.Requests;

public class LoginRequest
{
    [Required]
    [Display(Name = "EmailORUserName")]
    public string EmailORUserName { get; set; } = string.Empty;
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
