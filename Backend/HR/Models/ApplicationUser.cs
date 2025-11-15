using Microsoft.AspNetCore.Identity;

namespace HR.Models;

public class ApplicationUser : IdentityUser
{
    public Employee? Employee { get; set; }
}
