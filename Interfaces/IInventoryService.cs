using BirileriWebSitesi.Models.OrderAggregate;
using System.Reflection.Metadata;

namespace BirileriWebSitesi.Interfaces
{
    public interface IInventoryService
    {
        // if type = 1 it means add stock, if type = 2 it means remove stock
        Task<bool> UpdateInventoryAsync(Order order,int type);
        Task<bool> UpdateInventoryItemAsync(string productCode,int quantity, int type);
    }
}
