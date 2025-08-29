using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BirileriWebSitesi.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private string MyCart = string.Empty;
        public ProductService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> GetPriceAsync(string productCode)
        {
           decimal unitPrice = 0;
            bool discountExists = false;
            //get unitprice
            unitPrice = await _context.ProductVariants.Where(p => p.ProductCode == productCode)
                                                      .Select(p => p.Price)
                                                      .FirstOrDefaultAsync();
            //check if any discount
            discountExists = await _context.Discounts.Where(p => p.ProductCode == productCode &&
                                            p.StartDate <= DateTime.Now &&
                                            p.EndDate >= DateTime.Now)
                                    .AnyAsync();
            if (discountExists)
            {
                //get discounted price
                decimal discountedPrice = await _context.Discounts.Where(p => p.ProductCode == productCode &&
                                            p.StartDate <= DateTime.Now &&
                                            p.EndDate >= DateTime.Now)
                                    .Select(d=>d.DiscountAmount)        
                                    .FirstOrDefaultAsync();
                //get discount type
                string? discountType = await _context.Discounts.Where(p => p.ProductCode == productCode &&
                                            p.StartDate <= DateTime.Now &&
                                            p.EndDate >= DateTime.Now)
                                    .Select(d=>d.DiscountType)        
                                    .FirstOrDefaultAsync();
                //if discount type percentage calculate discounted price accordingly
                if (discountType == "Percentage")
                    discountedPrice = unitPrice * discountedPrice;
                //if discount exists return discounted price
                return discountedPrice;
            }
            //if discount not exists return unit price
            return unitPrice;
        }
        public async Task<string> GetProductNameAsync(string productCode)
        {
            try
            {

                string? productName = string.Empty;
                //get unitprice
                productName = await _context.ProductVariants.Where(p => p.ProductCode == productCode)
                                                          .Select(p => p.ProductName)
                                                          .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(productName))
                    productName = string.Empty;
                return productName;

            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        public async Task<string> GetImagePathAsync(string productCode)
        {
            try
            {
                string? imagePath = string.Empty;
                //get unitprice
                imagePath = await _context.ProductVariants.Where(p => p.ProductCode == productCode)
                                                          .Select(p => p.ImagePath)
                                                          .FirstOrDefaultAsync();

                if (string.IsNullOrEmpty(imagePath))
                    imagePath = string.Empty;
                return imagePath;

            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        public async Task<string> GetProductSlugAsync(string productCode)
        {
            try
            {
                string? slug = string.Empty;
                string? baseProductCode = string.Empty;
                
                if (productCode.Length > 12)
                    baseProductCode = await _context.ProductVariants.Where(p => p.ProductCode == productCode).Select(b => b.BaseProduct).FirstOrDefaultAsync();
                else
                    baseProductCode = productCode;
                
                slug = await _context.Products.Where(p => p.ProductCode == baseProductCode).Select(p => p.Slug).FirstOrDefaultAsync();
                return slug ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
