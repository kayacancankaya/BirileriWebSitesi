using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using Microsoft.EntityFrameworkCore;
using BirileriWebSitesi.Models.OrderAggregate;

namespace BirileriWebSitesi.Services
{
    public class InventoryService : IInventoryService
    {
		private ILogger<InventoryService> _logger;
        private ApplicationDbContext _context;

        public InventoryService(ILogger<InventoryService> logger,
                                ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<bool> UpdateInventoryAsync(Order order, int type)
        {
			try
			{

                foreach(var item in order.OrderItems)
                {

                    var sql = "CALL bpUpdateInventory(@p_productCode, @p_quantity, @p_type);";

                    var parameters = new[]
                    {
                        new MySql.Data.MySqlClient.MySqlParameter("@p_productCode", item.ProductCode),
                        new MySql.Data.MySqlClient.MySqlParameter("@p_quantity", item.Units),
                        new MySql.Data.MySqlClient.MySqlParameter("@p_type", type)  // 2 means minus stock 1 means plus stock
                    };

                    int affectedRows = await _context.Database.ExecuteSqlRawAsync(sql, parameters);
                    if (affectedRows == 0)
                         _logger.LogWarning("No rows affected for product code {ProductCode} with quantity {Quantity} and type {Type}.", item.ProductCode, item.Units, type);
                }                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update Inventory procedure failed orderId:{orderId}.",order.Id);
                return false;
            }
        }
        public async Task<bool> UpdateInventoryItemAsync(string productCode,int quantity, int type)
        {
			try
			{
                var sql = "CALL bpUpdateInventory(@p_productCode, @p_quantity, @p_type);";

                var parameters = new[]
                {
                    new MySql.Data.MySqlClient.MySqlParameter("@p_productCode", productCode),
                    new MySql.Data.MySqlClient.MySqlParameter("@p_quantity", quantity),
                    new MySql.Data.MySqlClient.MySqlParameter("@p_type", type)  // 2 means minus stock 1 means plus stock
                };

                int affectedRows = await _context.Database.ExecuteSqlRawAsync(sql, parameters);
                if (affectedRows == 0)
                     _logger.LogWarning("No rows affected for product code {ProductCode} with quantity {Quantity} and type {Type}.", productCode, quantity, type);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Update Inventory procedure failed productCode:{productCode}.",productCode);
                return false;
            }
        }
    }
}
