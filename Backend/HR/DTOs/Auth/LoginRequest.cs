using System.ComponentModel.DataAnnotations;

namespace HR.DTOs.Auth;

public class LoginRequest
{
    [Required]
    [StringLength(256)]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Password { get; set; } = string.Empty;
}
