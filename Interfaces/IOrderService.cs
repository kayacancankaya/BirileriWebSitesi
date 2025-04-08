using BirileriWebSitesi.Models.OrderAggregate;

namespace BirileriWebSitesi.Interfaces
{
    public interface IOrderService
    {
        Task<Dictionary<Address, Address>> GetAddress(string userId); 
        Task<string> ProcessOrderAsync(Order order); 

    }
}
