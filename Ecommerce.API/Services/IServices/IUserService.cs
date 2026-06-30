using System.Security.Claims;

namespace Ecommerce.API.Services.IServices;

public interface IUserService
{
    string? GetUserId(ClaimsPrincipal claim);
}
