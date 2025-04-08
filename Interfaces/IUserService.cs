using BirileriWebSitesi.Models.OrderAggregate;
using System.Security.Claims;

namespace BirileriWebSitesi.Interfaces
{
    public interface IUserService
    {
        Task<bool> UpdateUserAsync(Order order, ClaimsPrincipal user);
    }
}
