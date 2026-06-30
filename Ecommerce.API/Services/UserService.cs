using System.Security.Claims;

namespace Ecommerce.API.Services;

public class UserService : IUserService
{
    public string? GetUserId(ClaimsPrincipal claim)
    {
        return claim.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}
