using BirileriWebSitesi.Models;

namespace BirileriWebSitesi.Interfaces
{
    public interface IUserAuditService
    {
        public Task<bool> UpdateLoginInfo(string userId, DateTime registrationDate);
        public Task<bool> CreateUserAudit(string userId, DateTime lastLoginDate);
        public Task<bool> UpdateIpInfo(string userId, string ip);
        public Task<bool> IsInBuyRegion(string userId, string ip);
        public Task<UserAudit> GetUsurAuditAsync(string userId);
    }
}
