using BirileriWebSitesi.Models.InquiryAggregate;
using BirileriWebSitesi.Models.OrderAggregate;

namespace BirileriWebSitesi.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string email, string subject, string htmlMessage);
        Task<string> SendContactUsEmailAsync(string username, string email, string? phone, string message, string? subject);
        Task<bool> SendPaymentEmailAsync(string to,int orderId,string transferType);
        Task<bool> SendBankTransferNoticeEmailAsync(Order order, string note);
        Task<bool> SendCustomerOrderMailAsync(Order order);
        Task<bool> SendInquiryEmailAsync(string email, Inquiry inquiry);
    }
}
