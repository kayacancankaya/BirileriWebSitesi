using BirileriWebSitesi.Models;
using BirileriWebSitesi.Models.OrderAggregate;
using Iyzipay.Model;

namespace BirileriWebSitesi.Interfaces
{
    public interface IOrderService
    {
        Task<Dictionary<Models.OrderAggregate.Address, Models.OrderAggregate.Address>> GetAddress(string userId); 
        Task<string> ProcessOrderAsync(PaymentRequestModel payment);
        Task<string> Process3DsOrderAsync(PaymentRequestModel payment);
        Task<string> SaveOrderInfoAsync(Order order);
        Task<int> GetOrderID(Order order);
        Task<Order> GetOrderAsync(int orderId);
        Task<InstallmentDetail> GetInstallmentInfoAsync(string binNumber, decimal price);
        Task<bool> RecordPayment(PaymentLog payment);
        Task<bool> UpdateOrderStatus(int orderID,string status, int paymentType);
        Task<Dictionary<int,string>> GetBankTransferOrdersForUserAsync(string userID);
        Task<int> CancelOrderAsync(int orderId);
        Task<int> CancelOrderItemAsync(int orderId,string productCode);
        Task<bool> UpdateAddressAsync(Models.OrderAggregate.Address address);
        Task<bool> DeleteAddressAsync(int addressId);
        Task<float> GetDesiAsync(string productCode);
        Task<string> GetShipmentCompanyAsync(int orderId);  
    }
}
