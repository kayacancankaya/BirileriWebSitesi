﻿using BirileriWebSitesi.Models.BasketAggregate;

namespace BirileriWebSitesi.Interfaces
{
    public interface IBasketService
    {
        Task<Dictionary<int,string>> AddItemToBasketAsync(string userId, string productCode, decimal price, int quantity);
        Task<Basket> AddItemToAnonymousBasketAsync(Basket basket,string productCode, decimal price, int quantity);
        Task<int> CountTotalBasketItems(string userId);
        Task<int> CountDistinctBasketItems(string userId);
        Task DeleteBasketAsync(string userID);
        Task DeleteBasketAsync(int orderID);
        Task<bool> RemoveBasketItemAsync(string userID,string productCode);
        Task<Basket> SetQuantity(string userID,string productCode,int quantity); 
        Task TransferBasketAsync(string cart, string userID);
        Task<Basket> GetBasketAsync(string userID);
        
    }
}
