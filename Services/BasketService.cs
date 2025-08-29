using Azure;
using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models.BasketAggregate;
using BirileriWebSitesi.Models.InquiryAggregate;
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
        private readonly IProductService _productService;
        private readonly ILogger<BasketService> _logger;
      
        public BasketService(ApplicationDbContext context,
                             IProductService productService,
                             ILogger<BasketService> logger)
        {
            _context = context;
            _productService = productService;
            _logger = logger;
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

                string productName = await _productService.GetProductNameAsync(productCode);
                string imagePath = await _productService.GetImagePathAsync(productCode);    
                string slug = await _productService.GetProductSlugAsync(productCode);
                basket.AddItem(productCode, price, quantity,userId,productName,imagePath,slug);
                var basketItem = basket.Items.Where(i => i.BuyerID == userId &&
                                                                        i.ProductCode == productCode)
                                                                    .FirstOrDefault();
                basketItem.ProductVariant = await _context.ProductVariants.Where(p => p.ProductCode == productCode).FirstOrDefaultAsync();
                basket = UpdateTotals(basket, quantity, quantity * price, "Add");
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
        public async Task<Dictionary<int,string>> AddItemToInquiryBasketAsync(string userId, string productCode, decimal price, int quantity)
        {
            try
            {

                Inquiry? basket = null;
                Dictionary<int, string> result = new();
                if (userId == "0")
                {
                    result.Add(0, "Ürün Sepete Eklenirken Hata İle Karşılaşıldı");
                    return result;
                }

                basket = await _context.Inquiries.Where(b => b.BuyerId == userId)
                                                  .Include(b => b.Items).FirstOrDefaultAsync();

                if (basket == null)
                {
                    basket = new Inquiry(userId);
                    await _context.Inquiries.AddAsync(basket);
                    await _context.SaveChangesAsync();
                }

                string productName = await _productService.GetProductNameAsync(productCode);
                string imagePath = await _productService.GetImagePathAsync(productCode);    
                string slug = await _productService.GetProductSlugAsync(productCode);
                basket.AddItem(productCode, price, quantity,userId,productName,imagePath,slug);
                var basketItem = basket.Items.Where(i => i.BuyerID == userId &&
                                                                        i.ProductCode == productCode)
                                                                    .FirstOrDefault();
                basketItem.ProductVariant = await _context.ProductVariants.Where(p => p.ProductCode == productCode).FirstOrDefaultAsync();
                basket = UpdateInquiryTotals(basket, quantity, quantity * price, "Add");
                _context.Inquiries.Update(basket);
                await _context.SaveChangesAsync();
                int totalDistinctProduct = await CountDistinctInquiryBasketItems(userId);
                result.Add(totalDistinctProduct, "Ürün İstek Sepetine Eklendi.");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,ex.Message.ToString());
                Dictionary<int,string> result = new Dictionary<int,string>();
                result.Add(0, "Ürün İstek Sepetine Eklenirken Hata İle Karşılaşıldı");
                return result;
            }

        }
        public async Task<Basket> AddItemToAnonymousBasketAsync(Basket basket,string productCode, decimal price, int quantity)
        {
            try
            {
                string productName = await _productService.GetProductNameAsync(productCode);
                string imagePath = await _productService.GetImagePathAsync(productCode);
                string slug = await _productService.GetProductSlugAsync(productCode);
                basket.AddItem(productCode, price, quantity, "0", productName, imagePath, slug);
                foreach (var item in basket.Items)
                {
                    if(item.ProductCode == productCode)
                    {
                        item.ProductVariant = await _context.ProductVariants.Where(p => p.ProductCode == productCode).FirstOrDefaultAsync();
                        break;
                    }
                }
                basket = UpdateTotals(basket, quantity, quantity * price, "Add");
                return basket;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return basket;
            }

        }
        public async Task<Inquiry> AddItemToAnonymousInquiryBasketAsync(Inquiry basket, string productCode, decimal price, int quantity)
        {
            try
            {
                string productName = await _productService.GetProductNameAsync(productCode);
                string imagePath = await _productService.GetImagePathAsync(productCode);
                string slug = await _productService.GetProductSlugAsync(productCode);
                basket.AddItem(productCode, price, quantity, "0", productName, imagePath, slug);
                foreach (var item in basket.Items)
                {
                    if (item.ProductCode == productCode)
                    {
                        item.ProductVariant = await _context.ProductVariants.Where(p => p.ProductCode == productCode).FirstOrDefaultAsync();
                        break;
                    }
                }
                basket = UpdateInquiryTotals(basket, quantity, quantity * price, "Add");
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
        public async Task<int> CountTotalInquiryBasketItems(string userId)
        {
            try
            {

            var totalItems = await _context.Inquiries
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
        public async Task<int> CountDistinctInquiryBasketItems(string userId)
        {
            try
            {
                var totalDistinctItems = await _context.Inquiries
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
        public async Task DeleteInquiryBasketAsync(string userID)
        {
            try
            {

                var basket = await _context.Inquiries.Where(i => i.BuyerId == userID).FirstOrDefaultAsync();
                if (basket != null)
                {
                    _context.Inquiries.Remove(basket);
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
        public async Task<Inquiry> GetInquiryBasketAsync(string userID)
        {
            try
            {

                var basket = await _context.Inquiries.Where(i => i.BuyerId == userID)
                                    .Include(i=>i.Items)
                                    .ThenInclude(v=>v.ProductVariant)
                                    .FirstOrDefaultAsync();
                if (basket == null)
                {
                    basket = new Inquiry(userID);

                    await _context.Inquiries.AddAsync(basket);
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
                int totalQuantity = basketItem.Quantity;
                decimal totalAmount = basketItem.UnitPrice * basketItem.Quantity;
                if (basketItem != null)
                {
                    _context.BasketItems.Remove(basketItem);
                    var basket = await _context.Baskets.Where(b => b.BuyerId == userID)
                                                      .Include(b => b.Items)
                                                      .FirstOrDefaultAsync();
                    if (basket != null)
                    {
                        basket = UpdateTotals(basket,totalQuantity,totalAmount,"Remove");
                        _context.Baskets.Update(basket);
                    }
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
        public async Task<bool> RemoveInquiryBasketItemAsync(string userID,string productCode)
        {
            try
            {

                var basketItem = await _context.InquiryItems.Where(i => i.BuyerID == userID &&
                                                                        i.ProductCode == productCode)
                                                                    .FirstOrDefaultAsync();
                int totalQuantity = basketItem.Quantity;
                decimal totalAmount = basketItem.UnitPrice * basketItem.Quantity;
                if (basketItem != null)
                {
                    _context.InquiryItems.Remove(basketItem);
                    var basket = await _context.Inquiries.Where(b => b.BuyerId == userID)
                                                      .Include(b => b.Items)
                                                      .FirstOrDefaultAsync();
                    if (basket != null)
                    {
                        basket = UpdateInquiryTotals(basket,totalQuantity,totalAmount,"Remove");
                        _context.Inquiries.Update(basket);
                    }
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

            decimal price = basketItem?.UnitPrice ?? 0;
            int oldQuantity = basketItem?.Quantity ?? 0;
            int quantityDifference = quantity - oldQuantity;
            decimal totalAmountDifference = price * quantityDifference;
            decimal newTotalAmount = basket.TotalAmount + totalAmountDifference;
            
            basketItem.SetQuantity(quantity);
            basket = UpdateTotals(basket, quantityDifference, totalAmountDifference, "Add");
            basket.RemoveEmptyItems();
            _context.Baskets.Update(basket);
            await _context.SaveChangesAsync();
            return basket;

        }
        public async Task<Inquiry> SetInquiryQuantity(string userID, string productCode, int quantity)
        {
            var basket = await _context.Inquiries.Where(b=>b.BuyerId == userID)
                                              .Include(b=>b.Items)
                                              .ThenInclude(p=>p.ProductVariant)
                                              .FirstOrDefaultAsync();
            var basketItem = basket.Items
                            .Where(p=>p.ProductCode == productCode)
                            .FirstOrDefault();
            if (basket == null) return null;
            if (basketItem == null) return null;

            decimal price = basketItem?.UnitPrice ?? 0;
            int oldQuantity = basketItem?.Quantity ?? 0;
            int quantityDifference = quantity - oldQuantity;
            decimal totalAmountDifference = price * quantityDifference;
            decimal newTotalAmount = basket.TotalAmount + totalAmountDifference;
            
            basketItem.SetQuantity(quantity);
            basket = UpdateInquiryTotals(basket, quantityDifference, totalAmountDifference, "Add");
            basket.RemoveEmptyItems();
            _context.Inquiries.Update(basket);
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
                Basket basket = await _context.Baskets.Where(b => b.BuyerId == userID)
                                                        .Include(i=>i.Items)
                                                        .FirstOrDefaultAsync();
                if (basket == null)
                    basket = new(userID);
                else
                    isBasketExists = true;
                decimal unitPrice = 0;
                foreach (var item in result)
                {

                    unitPrice = await _productService.GetPriceAsync(item.Key);
                    string productName = await _productService.GetProductNameAsync(productCode);
                    string imagePath = await _productService.GetImagePathAsync(productCode);
                    string slug = await _productService.GetProductSlugAsync(productCode);
                    basket.AddItem(item.Key, unitPrice, quantity, userID, productName, imagePath, slug);
                    basket = UpdateTotals(basket, item.Value, unitPrice * item.Value, "Add");
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
        public async Task TransferInquiryBasketAsync(string cart, string userID)
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
                Inquiry basket = await _context.Inquiries.Where(b => b.BuyerId == userID)
                                                           .Include(i=>i.Items)
                                                           .FirstOrDefaultAsync();
                if (basket == null)
                    basket = new(userID);
                else
                    isBasketExists = true;
                decimal unitPrice = 0;
                foreach (var item in result)
                {

                     unitPrice = await _productService.GetPriceAsync(item.Key);
                    string productName = await _productService.GetProductNameAsync(productCode);
                    string imagePath = await _productService.GetImagePathAsync(productCode);
                    string slug = await _productService.GetProductSlugAsync(productCode);
                    basket.AddItem(item.Key, unitPrice, quantity, userID, productName, imagePath, slug);
                    basket = UpdateInquiryTotals(basket, item.Value, unitPrice * item.Value, "Add");
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
        private Basket UpdateTotals(Basket basket, int totalQuantity, decimal totalAmount, string action)
        {
            if (action == "Add")
            {
                basket.TotalItems += totalQuantity;
                basket.TotalAmount += totalAmount;
            }
            else if (action == "Remove")
            {
                basket.TotalItems -= totalQuantity;
                basket.TotalAmount -= totalAmount;
            }
            return basket;
        }
        private Inquiry UpdateInquiryTotals(Inquiry basket, int totalQuantity, decimal totalAmount, string action)
        {
            if (action == "Add")
            {
                basket.TotalItems += totalQuantity;
                basket.TotalAmount += totalAmount;
            }
            else if (action == "Remove")
            {
                basket.TotalItems -= totalQuantity;
                basket.TotalAmount -= totalAmount;
            }
            return basket;
        }
    }
}
