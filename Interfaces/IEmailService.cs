namespace BirileriWebSitesi.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string email, string subject, string htmlMessage);
        Task<string> SendContactUsEmailAsync(string username, string email, string? phone, string message, string? subject);
    }
}
