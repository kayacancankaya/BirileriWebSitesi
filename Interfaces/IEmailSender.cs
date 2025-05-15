namespace BirileriWebSitesi.Interfaces
{
    public interface IEmailSender
    {
        Task<bool> SendEmailAsync(string email, string subject, string htmlMessage, string from);
        Task<bool> SendContactUsEmailAsync(string username, string email, string? phone, string message, string? subject);
    }
}
