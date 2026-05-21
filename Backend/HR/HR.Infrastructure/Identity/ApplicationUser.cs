using Microsoft.AspNetCore.Identity;
using HR.Domain.Entities;

namespace HR.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public Employee? Employee { get; set; }
}
