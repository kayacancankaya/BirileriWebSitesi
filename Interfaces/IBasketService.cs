using BirileriWebSitesi.Models.BasketAggregate;
using BirileriWebSitesi.Models.InquiryAggregate;

namespace BirileriWebSitesi.Interfaces
{
    public interface IBasketService
    {
        Task<Dictionary<int,string>> AddItemToBasketAsync(string userId, string productCode, decimal price, int quantity);
        Task<Dictionary<int,string>> AddItemToInquiryBasketAsync(string userId, string productCode, decimal price, int quantity);
        Task<Basket> AddItemToAnonymousBasketAsync(Basket basket,string productCode, decimal price, int quantity);
        Task<Inquiry> AddItemToAnonymousInquiryBasketAsync(Inquiry basket,string productCode, decimal price, int quantity);
        Task<int> CountTotalBasketItems(string userId);
        Task<int> CountTotalInquiryBasketItems(string userId);
        Task<int> CountDistinctBasketItems(string userId);
        Task<int> CountDistinctInquiryBasketItems(string userId);
        Task DeleteBasketAsync(string userID);
        Task DeleteInquiryBasketAsync(string userID);
        Task DeleteBasketAsync(int orderID);
        Task<bool> RemoveBasketItemAsync(string userID,string productCode);
        Task<bool> RemoveInquiryBasketItemAsync(string userID,string productCode);
        Task<Basket> SetQuantity(string userID,string productCode,int quantity); 
        Task<Inquiry> SetInquiryQuantity(string userID,string productCode,int quantity); 
        Task TransferBasketAsync(string cart, string userID);
        Task TransferInquiryBasketAsync(string cart, string userID);
        Task<Inquiry> GetInquiryBasketAsync(string userID);
        Task<Basket> GetBasketAsync(string userID);
        
    }
}
