using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models.OrderAggregate;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace BirileriWebSitesi.Services
{
    public class UserService : IUserService
    {

        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<UserService> _logger;
        public UserService(UserManager<IdentityUser> user, ILogger<UserService> logger)
        {
            _userManager = user;
            _logger = logger;
        }
        public async Task<bool> UpdateUserAsync(Order order, ClaimsPrincipal user)
        {
            try
            {
                if (order == null)
                    return false;

                if (user == null)
                    return false;

                var appUser = await _userManager.FindByIdAsync(order.BuyerId);
                if (appUser == null)
                    return false;

                appUser.UserName = string.Format("{0} {1}",order.ShipToAddress.FirstName, order.ShipToAddress.LastName);
                appUser.NormalizedUserName = appUser.UserName.ToUpper();
                appUser.PhoneNumber = order.ShipToAddress.Phone;
                appUser.Email = order.ShipToAddress.EmailAddress;
                appUser.NormalizedEmail = appUser.Email.ToUpper();

                var result = await _userManager.UpdateAsync(appUser);
                return result.Succeeded;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return false;
            }
        }
    }
}
