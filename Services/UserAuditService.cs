using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace BirileriWebSitesi.Services
{
    public class UserAuditService : IUserAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly IOptions<IpInfoSettings> _ipInfoSettings;
        public UserAuditService(ApplicationDbContext context, 
                                ILogger<OrderService> logger,
                                IOptions<IpInfoSettings> ipInfoSettings)
        {
            _context = context;
            _logger = logger;
            _ipInfoSettings = ipInfoSettings;
        }
        public async Task<bool> CreateUserAudit(string userId, DateTime registrationDate, string ip)
        {
            try
            {
                UserAudit userAudit = new UserAudit
                {
                    UserId = userId,
                    RegistrationDate = registrationDate,
                    LastLoginDate = registrationDate
                };
                var existingUserAudit = await _context.UserAudits.FirstOrDefaultAsync(x => x.UserId == userId);
                if (existingUserAudit == null)
                {
                    await _context.UserAudits.AddAsync(userAudit);
                }
                else
                {
                    existingUserAudit.RegistrationDate = registrationDate;
                    existingUserAudit.LastLoginDate = registrationDate;
                
                    _context.UserAudits.Update(existingUserAudit);
                }
                if (!string.IsNullOrEmpty(ip) &&
                    ip != "::1")
                {
                    HttpClient client = new HttpClient();
                    var ipInfoSettings = _ipInfoSettings.Value;
                    var response = await client.GetStringAsync($"https://ipinfo.io/{ip}?token={ipInfoSettings.Token}");
                    var ipInfo = JsonConvert.DeserializeObject<IpInfoResponse>(response);
                    userAudit.Ip = ip;
                    userAudit.City = ipInfo?.City;
                    userAudit.Country = ipInfo?.Country;
                }
                var result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return true;
                }
                else
                {
                    _logger.LogError("Kullanıcı Kaydı Oluşturulamadı.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,ex.Message.ToString());
                return false;
            }
        }
        public async Task<bool> UpdateLoginInfo(string userId, DateTime lastLoginDate, string ip)
        {
            try
            {
                var existingUserAudit = await _context.UserAudits.FirstOrDefaultAsync(x => x.UserId == userId);
                if (existingUserAudit == null)
                {

                    UserAudit userAudit = new UserAudit
                    {
                        UserId = userId,
                        RegistrationDate = lastLoginDate,
                        LastLoginDate = lastLoginDate
                    };
                    await _context.UserAudits.AddAsync(userAudit);
                }
                else
                {
                    existingUserAudit.LastLoginDate = lastLoginDate;
                    _context.UserAudits.Update(existingUserAudit);
                }

                if (!string.IsNullOrEmpty(ip) &&
                    ip != "::1")
                {
                    HttpClient client = new HttpClient();
                    var ipInfoSettings = _ipInfoSettings.Value;
                    var response = await client.GetStringAsync($"https://ipinfo.io/{ip}?token={ipInfoSettings.Token}");
                    var ipInfo = JsonConvert.DeserializeObject<IpInfoResponse>(response);
                    existingUserAudit.Ip = ip;
                    existingUserAudit.City = ipInfo?.City;
                    existingUserAudit.Country = ipInfo?.Country;
                }

                var result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return true;
                }
                else
                {
                    _logger.LogError("Kullanıcı Kaydı Güncellenemedi.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return false;
            }
        }
        public async Task<bool> UpdateIpInfo(string userId , string ip)
        {
            try
            {
                var ipInfoSettings = _ipInfoSettings.Value;
                var existingUserAudit = await _context.UserAudits.FirstOrDefaultAsync(x => x.UserId == userId);
                if (existingUserAudit == null)
                    return false;   
               HttpClient client = new HttpClient();
                var response = await client.GetStringAsync($"https://ipinfo.io/{ip}?token={ipInfoSettings.Token}");
               
                var ipInfo = JsonConvert.DeserializeObject<IpInfoResponse>(response);

                existingUserAudit.Ip = ip;
                existingUserAudit.City = ipInfo?.City;
                existingUserAudit.Country = ipInfo?.Country;
                _context.UserAudits.Update(existingUserAudit);
                var result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return true;
                }
                else
                {
                    _logger.LogError("Kullanıcı IP Bilgisi Güncellenemedi.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return false;
            }
        }
        public async Task<bool> IsInBuyRegion(string userId, string ip)
        {
            try
            {

                var ipInfoSettings = _ipInfoSettings.Value;
                var existingUserAudit = await _context.UserAudits.FirstOrDefaultAsync(x => x.UserId == userId);
               
                
                if (existingUserAudit == null)
                    return false;
                HttpClient client = new HttpClient();
                var response = await client.GetStringAsync($"https://ipinfo.io/{ip}?token={ipInfoSettings.Token}");
                _logger.LogWarning("IP Bilgisi talep edildi");
                var ipInfo = JsonConvert.DeserializeObject<IpInfoResponse>(response);

                existingUserAudit.Ip = ip;
                existingUserAudit.City = ipInfo?.City == null ? string.Empty : ipInfo.City;
                existingUserAudit.Country = ipInfo?.Country == null ? string.Empty : ipInfo.Country;
        
                _context.UserAudits.Update(existingUserAudit);
                var result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    if (ipInfo?.Country == "Türkiye" || ipInfo?.Country == "Turkey" || ipInfo?.Country == "Turkiye"
                        || ipInfo?.Country == "TR")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    _logger.LogError("Kullanıcı IP Bilgisi Güncellenemedi.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return false;
            }
        }
        public async Task<UserAudit> GetUsurAuditAsync(string userId)
        {
            try
            {
                var existingUserAudit = await _context.UserAudits.FirstOrDefaultAsync(x => x.UserId == userId);
                return existingUserAudit;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return null;
            }
        }
    }
}
