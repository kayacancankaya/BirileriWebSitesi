using Azure;
using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models.BasketAggregate;
using BirileriWebSitesi.Models.OrderAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using NuGet.ContentModel;
using System;
using System.Net;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace BirileriWebSitesi.Services
{
    public class BasketService : IBasketService
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BasketService> _logger;
        private string MyCart = string.Empty;
        public BasketService(ApplicationDbContext context,
                             IServiceProvider serviceProvider)
        {
            _context = context;
            _serviceProvider = serviceProvider;
        }

        public async Task<Dictionary<int,string>> AddItemToBasketAsync(string userId, string productCode, decimal price, int quantity)
        {
            try
            {

                Basket? basket = null;
                Dictionary<int, string> result = new();
                if (userId == "0")
                {
                    result.Add(0, "Ürün Sepete Eklenirken Hata İle Karşılaşıldı");
                    return result;
                }

                basket = await _context.Baskets.Where(b => b.BuyerId == userId)
                                                  .Include(b => b.Items).FirstOrDefaultAsync();

                if (basket == null)
                {
                    basket = new Basket(userId);
                    await _context.Baskets.AddAsync(basket);
                    await _context.SaveChangesAsync();
                }
                var _productService = _serviceProvider.GetRequiredService<IProductService>();
                string productName = await _productService.GetProductNameAsync(productCode);
                string imagePath = await _productService.GetImagePathAsync(productCode);    
                basket.AddItem(productCode, price, quantity,userId,productName,imagePath);
                var basketItem = basket.Items.Where(i => i.BuyerID == userId &&
                                                                        i.ProductCode == productCode)
                                                                    .FirstOrDefault();
                basketItem.ProductVariant = await _context.ProductVariants.Where(p => p.ProductCode == productCode).FirstOrDefaultAsync();

                _context.Baskets.Update(basket);
                await _context.SaveChangesAsync();
                int totalDistinctProduct = await CountDistinctBasketItems(userId);
                result.Add(totalDistinctProduct, "Ürün Sepete Eklendi.");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,ex.Message.ToString());
                Dictionary<int,string> result = new Dictionary<int,string>();
                result.Add(0, "Ürün Sepete Eklenirken Hata İle Karşılaşıldı");
                return result;
            }

        }
        public async Task<Basket> AddItemToAnonymousBasketAsync(Basket basket,string productCode, decimal price, int quantity)
        {
            try
            {
                var _productService = _serviceProvider.GetRequiredService<IProductService>();
                string productName = await _productService.GetProductNameAsync(productCode);
                string imagePath = await _productService.GetImagePathAsync(productCode);
                basket.AddItem(productCode, price, quantity, "0", productName, imagePath);
                foreach (var item in basket.Items)
                {
                    if(item.ProductCode == productCode)
                    {
                        item.ProductVariant = await _context.ProductVariants.Where(p => p.ProductCode == productCode).FirstOrDefaultAsync();
                        break;
                    }
                }

                return basket;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return basket;
            }

        }

        public async Task<int> CountTotalBasketItems(string userId)
        {
            try
            {

            var totalItems = await _context.Baskets
                                    .Where(basket => basket.BuyerId == userId)
                                    .SelectMany(item => item.Items)
                                    .SumAsync(sum => sum.Quantity);

            return totalItems;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return -1;
            }
        }
        
        public async Task<int> CountDistinctBasketItems(string userId)
        {
            try
            {
                var totalDistinctItems = await _context.Baskets
                                        .Where(basket => basket.BuyerId == userId)
                                        .SelectMany(item => item.Items)
                                        .CountAsync();

                return totalDistinctItems;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return -1;
            }
        }

        public async Task DeleteBasketAsync(string userID)
        {
            try
            {

                var basket = await _context.Baskets.Where(i => i.BuyerId == userID).FirstOrDefaultAsync();
                if (basket != null)
                {
                    _context.Baskets.Remove(basket);
                    await _context.SaveChangesAsync();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
            }
        }
        public async Task DeleteBasketAsync(int orderID)
        {
            try
            {
                var userID = await _context.Orders.Where(i => i.Id == orderID).Select(b => b.BuyerId).FirstOrDefaultAsync();
                var basket = await _context.Baskets.Where(i => i.BuyerId == userID).FirstOrDefaultAsync();
                if (basket != null)
                {
                    _context.Baskets.Remove(basket);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
            }
            
        }
        public async Task<Basket> GetBasketAsync(string userID)
        {
            try
            {

                var basket = await _context.Baskets.Where(i => i.BuyerId == userID)
                                    .Include(i=>i.Items)
                                    .ThenInclude(v=>v.ProductVariant)
                                    .FirstOrDefaultAsync();
                if (basket == null)
                {
                    basket = new Basket(userID);
                    await _context.Baskets.AddAsync(basket);
                    await _context.SaveChangesAsync();
                }
                return basket;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return null;
            }
        }
        public async Task<bool> RemoveBasketItemAsync(string userID,string productCode)
        {
            try
            {

                var basketItem = await _context.BasketItems.Where(i => i.BuyerID == userID &&
                                                                        i.ProductCode == productCode)
                                                                    .FirstOrDefaultAsync();
                if (basketItem != null)
                {
                    _context.BasketItems.Remove(basketItem);
                     await _context.SaveChangesAsync();
                    return true;
                }
                return false;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return false;
            }
        }
    
        public async Task<Basket> SetQuantities(string userID, Dictionary<string, int> quantities)
        {
            try
            {

                var basket = await _context.Baskets.Where(b=>b.BuyerId == userID)
                                                  .Include(b=>b.Items)
                                                  .FirstOrDefaultAsync();

                if (basket == null) return null;

                foreach (var item in basket.Items)
                {
                    if (quantities.TryGetValue(item.ProductCode, out var quantity))
                    {
                        item.SetQuantity(quantity);
                    }
                }
                basket.RemoveEmptyItems();
                _context.Baskets.Update(basket);
                await _context.SaveChangesAsync();
                return basket;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return null;
            }

        }
        public async Task<Basket> SetQuantity(string userID, string productCode, int quantity)
        {
            var basket = await _context.Baskets.Where(b=>b.BuyerId == userID)
                                              .Include(b=>b.Items)
                                              .ThenInclude(p=>p.ProductVariant)
                                              .FirstOrDefaultAsync();
            var basketItem = basket.Items
                            .Where(p=>p.ProductCode == productCode)
                            .FirstOrDefault();

            if (basket == null) return null;
            if (basketItem == null) return null;

            basketItem.SetQuantity(quantity);
            
            basket.RemoveEmptyItems();
            _context.Baskets.Update(basket);
            _context.BasketItems.Update(basketItem);
            await _context.SaveChangesAsync();
            return basket;

        }

        public async Task TransferBasketAsync(string cart, string userID)
        {
            try
            {
                bool isBasketExists = false;
                string[] MyCartArray = cart.Split('&');
                Dictionary<string, int> result = new();
                string productCode = string.Empty;
                int quantity = 0;
                foreach (string item in MyCartArray)
                {
                    string[] existingID = item.Split(';');

                    if (string.IsNullOrEmpty(existingID[0]))
                        productCode = string.Empty;
                    else
                        productCode = existingID[0];
                    if (Int32.TryParse(existingID[1], out quantity) == false)
                        quantity = 0;
                    else
                        quantity = Convert.ToInt32(existingID[1]);

                    result.Add(productCode, quantity);

                }
                Basket basket = await _context.Baskets.Where(b => b.BuyerId == userID).FirstOrDefaultAsync();
                if (basket == null)
                    basket = new(userID);
                else
                    isBasketExists = true;
                decimal unitPrice = 0;
                foreach (var item in result)
                {

                    var _productService = _serviceProvider.GetRequiredService<IProductService>();
                    unitPrice = await _productService.GetPriceAsync(item.Key);
                    string productName = await _productService.GetProductNameAsync(productCode);
                    string imagePath = await _productService.GetImagePathAsync(productCode);
                    basket.AddItem(item.Key, unitPrice, quantity, userID, productName, imagePath);
                    foreach (var basketItem in basket.Items)
                    {
                        if (basketItem.ProductCode == item.Key)
                        {
                            basketItem.ProductVariant = await _context.ProductVariants.Where(p => p.ProductCode == item.Key).FirstOrDefaultAsync();
                            break;
                        }
                    }
                }
                if(isBasketExists)
                    _context.Update(basket);
                else
                    _context.Add(basket);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
            }
        }
    }
}
