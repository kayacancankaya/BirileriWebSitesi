using Azure.Core;
using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models;
using BirileriWebSitesi.Models.OrderAggregate;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Query.Internal;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Composition;
using System.Globalization;
using static BirileriWebSitesi.Models.Enums.AprrovalStatus;
using Address = BirileriWebSitesi.Models.OrderAggregate.Address;
using OrderItem = BirileriWebSitesi.Models.OrderAggregate.OrderItem;

namespace BirileriWebSitesi.Services
{
    public class OrderService : IOrderService
    {

        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly IIyzipayPaymentService _iyziPay;
        private readonly IInventoryService _inventoryService;

        public OrderService(ApplicationDbContext context, ILogger<OrderService> logger,
                                UserManager<IdentityUser> user,
                                IIyzipayPaymentService iyzipay,
                                IInventoryService inventoryService)
        {
            _context = context;
            _logger = logger;
            _iyziPay = iyzipay;
            _inventoryService = inventoryService;
        }

        public Task<Dictionary<Address, Address>> GetAddress(string userId)
        {
            throw new NotImplementedException();
        }
        public async Task<string> SaveOrderInfoAsync(Order order)
        {
            try
            {
                if (order.ShipToAddress == null)
                    return "Gönderilecek Adres Bulunamadı.";
                if (string.IsNullOrEmpty(order.ShipToAddress.FirstName))
                    return "Gönderilecek Adres İsim Bulunamadı.";
                if (string.IsNullOrEmpty(order.ShipToAddress.LastName))
                    return "Gönderilecek Adres Soy İsim Bulunamadı.";
                if (string.IsNullOrEmpty(order.ShipToAddress.EmailAddress))
                    return "Gönderilecek Adres Email Bulunamadı.";
                if (string.IsNullOrEmpty(order.ShipToAddress.AddressDetailed))
                    return "Gönderilecek Adres Bilgisi Bulunamadı.";
                if (string.IsNullOrEmpty(order.ShipToAddress.City))
                    return "Gönderilecek Adres Şehir Bilgisi Bulunamadı.";
                if (string.IsNullOrEmpty(order.ShipToAddress.State))
                    return "Gönderilecek Adres İlçe Bilgisi Bulunamadı.";
                if (string.IsNullOrEmpty(order.ShipToAddress.Street))
                    return "Gönderilecek Adres Mahalle Bilgisi Bulunamadı.";

                if (order.BillingAddress == null)
                    return "Fatura Adresi Bulunamadı.";
                if (!order.BillingAddress.IsCorporate)
                {
                    if (string.IsNullOrEmpty(order.BillingAddress.FirstName))
                        return "Fatura Adresi İsim Bulunamadı.";
                    if (string.IsNullOrEmpty(order.BillingAddress.LastName))
                        return "Fatura Adresi Soy İsim Bulunamadı.";
                    order.BillingAddress.CorporateName = string.Empty;
                    order.BillingAddress.VATnumber = "11111111111";
                }
                else
                {
                    if (string.IsNullOrEmpty(order.BillingAddress.CorporateName))
                        return "Şirket İsmi Bulunamadı.";
                    if (string.IsNullOrEmpty(order.BillingAddress.VATstate))
                        return "Vergi Dairesi Bulunamadı.";
                    if (string.IsNullOrEmpty(order.BillingAddress.VATnumber))
                        return "Vergi Numarası Bulunamadı.";
                    order.BillingAddress.FirstName = string.Empty;
                    order.BillingAddress.LastName = string.Empty;
                }
                if (string.IsNullOrEmpty(order.BillingAddress.EmailAddress))
                    return "Fatura Adresi Email Bulunamadı.";
                if (string.IsNullOrEmpty(order.BillingAddress.AddressDetailed))
                    return "Fatura Adresi Bilgisi Bulunamadı.";
                if (string.IsNullOrEmpty(order.BillingAddress.City))
                    return "Fatura Adresi Şehir Bilgisi Bulunamadı.";
                if (string.IsNullOrEmpty(order.BillingAddress.State))
                    return "Fatura Adresi İlçe Bilgisi Bulunamadı.";
                if (string.IsNullOrEmpty(order.BillingAddress.Street))
                    return "Fatura Adresi Mahalle Bilgisi Bulunamadı.";

                if (order.UpdateUserInfo)
                {
                    order.ShipToAddress.SetAsDefault = true;
                    order.BillingAddress.SetAsDefault = true;
                }

                if (string.IsNullOrEmpty(order.ShipmentCode))
                    return "Kargo Seçiniz..." ;
                if (order.ShipmentCost == 0)
                    return "Kargo Miktar 0 olamaz...";
                //update address
                //check if addresses exists
                bool result = await CheckIfAddressExistsAsync(order.ShipToAddress);
                if (result)
                {
                    _context.Addresses.Update(order.ShipToAddress);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    await _context.Addresses.AddAsync(order.ShipToAddress);
                    await _context.SaveChangesAsync();
                }
                result = await CheckIfAddressExistsAsync(order.BillingAddress);
                if (result)
                {
                    _context.Addresses.Update(order.BillingAddress);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    await _context.Addresses.AddAsync(order.BillingAddress);
                    await _context.SaveChangesAsync();
                }


                foreach (Models.OrderAggregate.OrderItem item in order.OrderItems)
                {
                    item.ProductVariant = await _context.ProductVariants.Where(p => p.ProductCode == item.ProductCode).FirstOrDefaultAsync();
                    item.ProductVariant.Product = await _context.Products.Where(p => p.ProductCode == item.ProductVariant.BaseProduct)
                                                                             .Include(c => c.Catalog)
                                                                            .FirstOrDefaultAsync();
                }

                order.Status = (int)ApprovalStatus.Pending;

                _context.Add(order);

                await _context.SaveChangesAsync();

                return order.Id.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return "Sipariş Kaydedilirken Sistemsel Hata Oluştu, Lütfen Tekrar Deneyiniz.";
            }
        }
        public async Task<string> Process3DsOrderAsync(PaymentRequestModel model)
        {
            try
            {

                Order order = await GetOrderAsync(model.OrderId);
                string checkString = await CheckOrderAsync(order, model);

                if (checkString != "success")
                    return "Ödeme Esnasında Hata ile Karşılaşıldı.";

                string htmlResult = await _iyziPay.IyziPayCreate3dsReqAsync(order, model);

                if (htmlResult.TrimStart().StartsWith("<!doctype html>", StringComparison.OrdinalIgnoreCase))
                    order.Status = (int)ApprovalStatus.Pending;
                else
                    order.Status = (int)ApprovalStatus.Failed;

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();


                return htmlResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return "Ödeme Esnasında Hata ile Karşılaşıldı.";
            }
        }
        public async Task<string> ProcessOrderAsync(PaymentRequestModel model)
        {
            try
            {
                Order order = await GetOrderAsync(model.OrderId);
                string checkString = await CheckOrderAsync(order, model);

                if (checkString != "success")
                    return checkString;

                Payment payment = await _iyziPay.IyziPayCreateReqAsync(order, model);

                PaymentLog paymentLog = new();
                paymentLog.ConversationId = payment.ConversationId;
                paymentLog.OrderId = Convert.ToInt32(payment.BasketId);
                paymentLog.PaymentId = payment.PaymentId;
                paymentLog.Price = payment.Price;
                paymentLog.PaidPrice = payment.PaidPrice;
                paymentLog.IyziCommissionRateAmount = payment.IyziCommissionRateAmount;
                paymentLog.IyziCommissionFee = payment.IyziCommissionFee;
                paymentLog.CardFamily = payment.CardFamily;
                paymentLog.CardAssociation = payment.CardAssociation;
                paymentLog.CardType = payment.CardType;
                paymentLog.BinNumber = payment.BinNumber;
                paymentLog.LastFourDigits = payment.LastFourDigits;
                paymentLog.Status = payment.Status;
                paymentLog.PaidAt = DateTime.UtcNow;
                var paymentLogOrder = await _context.Orders.FindAsync(Convert.ToInt32(payment.BasketId));
                paymentLog.Order = paymentLogOrder;
                await RecordPayment(paymentLog);

                if (payment.Status == "success")
                    order.Status = (int)ApprovalStatus.Approved;
                else
                    order.Status = (int)ApprovalStatus.Failed;

                if (!string.IsNullOrEmpty(payment.ErrorMessage))
                    return payment.ErrorMessage.ToString();

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                if (payment.Status == "success")
                    return "success";
                else
                    return payment.ErrorMessage.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return "Sipariş Kaydedilirken Sistemsel Hata Oluştu, Lütfen Tekrar Deneyiniz.";
            }
        }
        public async Task<int> GetOrderID(Order order)
        {
            try
            {
                var orderId = await _context.Orders.Where(o => o.BuyerId == order.BuyerId && o.OrderDate == order.OrderDate).Select(o => o.Id).FirstOrDefaultAsync();
                return orderId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return 0;
            }
        }
        public async Task<InstallmentDetail> GetInstallmentInfoAsync(string binNumber, decimal price)
        {
            Iyzipay.Options options = await _iyziPay.GetIyzipayOptionsAsync();

            var request = new RetrieveInstallmentInfoRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = Guid.NewGuid().ToString(),
                BinNumber = binNumber,
                Price = price.ToString("F2", CultureInfo.InvariantCulture)
            };

            var result = await InstallmentInfo.Retrieve(request, options);

            var installmentDetails = result.InstallmentDetails.FirstOrDefault();

            return installmentDetails;
        }
        public async Task<Order> GetOrderAsync(int orderId)
        {
            try
            {
                Order order = await _context.Orders
                    .Include(o => o.ShipToAddress)
                    .Include(o => o.BillingAddress)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                    .ThenInclude(c => c.Catalog)
                    .FirstOrDefaultAsync(o => o.Id == orderId);
                return order;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return null;
            }
        }
        private async Task<bool> CheckIfAddressExistsAsync(Address address)
        {
            try
            {
                bool result = await _context.Addresses.Where(a => a.UserId == address.UserId &&
                                             a.FirstName == address.FirstName &&
                                             a.LastName == address.LastName &&
                                             a.EmailAddress == address.EmailAddress &&
                                             a.Phone == address.Phone &&
                                             a.City == address.City &&
                                             a.Street == address.Street &&
                                             a.ZipCode == address.ZipCode &&
                                             a.Country == address.Country &&
                                             a.State == address.State &&
                                             a.VATnumber == address.VATnumber &&
                                             a.VATstate == address.VATstate &&
                                             a.CorporateName == address.CorporateName)
                                             .AnyAsync();
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return false;
            }
        }
        private async Task<string> CheckOrderAsync(Order order, PaymentRequestModel model)
        {
            try
            {
                if (order.ShipToAddress == null)
                    return "Gönderilecek Adres Bulunamadı.";
                if (string.IsNullOrEmpty(order.ShipToAddress.FirstName))
                    return "Gönderilecek Adres İsim Bulunamadı.";
                if (string.IsNullOrEmpty(order.ShipToAddress.LastName))
                    return "Gönderilecek Adres Soy İsim Bulunamadı.";
                if (string.IsNullOrEmpty(order.ShipToAddress.EmailAddress))
                    return "Gönderilecek Adres Email Bulunamadı.";
                if (string.IsNullOrEmpty(order.ShipToAddress.AddressDetailed))
                    return "Gönderilecek Adres Bilgisi Bulunamadı.";
                if (string.IsNullOrEmpty(order.ShipToAddress.City))
                    return "Gönderilecek Adres Şehir Bilgisi Bulunamadı.";
                if (string.IsNullOrEmpty(order.ShipToAddress.State))
                    return "Gönderilecek Adres İlçe Bilgisi Bulunamadı.";
                if (string.IsNullOrEmpty(order.ShipToAddress.Street))
                    return "Gönderilecek Adres Mahalle Bilgisi Bulunamadı.";

                if (order.BillingAddress == null)
                    return "Fatura Adresi Bulunamadı.";
                if (!order.BillingAddress.IsCorporate)
                {
                    if (string.IsNullOrEmpty(order.BillingAddress.FirstName))
                        return "Fatura Adresi İsim Bulunamadı.";
                    if (string.IsNullOrEmpty(order.BillingAddress.LastName))
                        return "Fatura Adresi Soy İsim Bulunamadı.";
                    order.BillingAddress.CorporateName = string.Empty;
                    order.BillingAddress.VATnumber = "11111111111";
                }
                else
                {
                    if (string.IsNullOrEmpty(order.BillingAddress.CorporateName))
                        return "Şirket İsmi Bulunamadı.";
                    if (string.IsNullOrEmpty(order.BillingAddress.VATstate))
                        return "Vergi Dairesi Bulunamadı.";
                    if (string.IsNullOrEmpty(order.BillingAddress.VATnumber))
                        return "Vergi Numarası Bulunamadı.";
                    order.BillingAddress.FirstName = string.Empty;
                    order.BillingAddress.LastName = string.Empty;
                }
                if (string.IsNullOrEmpty(order.BillingAddress.EmailAddress))
                    return "Fatura Adresi Email Bulunamadı.";
                if (string.IsNullOrEmpty(order.BillingAddress.AddressDetailed))
                    return "Fatura Adresi Bilgisi Bulunamadı.";
                if (string.IsNullOrEmpty(order.BillingAddress.City))
                    return "Fatura Adresi Şehir Bilgisi Bulunamadı.";
                if (string.IsNullOrEmpty(order.BillingAddress.State))
                    return "Fatura Adresi İlçe Bilgisi Bulunamadı.";
                if (string.IsNullOrEmpty(order.BillingAddress.Street))
                    return "Fatura Adresi Mahalle Bilgisi Bulunamadı.";

                if (string.IsNullOrEmpty(model.CardHolderName))
                    return "Kredi Kartı Üzerindeki İsim Bulunamadı.";
                if (string.IsNullOrEmpty(model.CreditCardNumber))
                    return "Kredi Kartı Numarası Bulunamadı.";
                if (string.IsNullOrEmpty(model.ExpMonth))
                    return "Kredi Kartı Son Kullanma Tarihi Bulunamadı.";
                if (string.IsNullOrEmpty(model.ExpYear))
                    return "Kredi Kartı Son Kullanma Tarihi Bulunamadı.";
                if (string.IsNullOrEmpty(model.CVV))
                    return "Kredi Kartı CVV Bulunamadı.";

                return "success";

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return "Sipariş Kontrol Edilirken Sistemsel Hata Oluştu, Lütfen Tekrar Deneyiniz.";
            }
        }
        public async Task<bool> RecordPayment(PaymentLog payment)
        {
            try
            {
                _context.PaymentLogs.Add(payment);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return false;
            }
        }
        public async Task<bool> UpdateOrderStatus(int orderID, string status, int paymentType)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderID);
                if (order != null)
                {
                    switch (status)
                    {
                        case "Pending":
                            order.Status = (int)ApprovalStatus.Pending;
                            break;
                        case "Approved":
                            order.Status = (int)ApprovalStatus.Approved;
                            break;
                        case "Failed":
                            order.Status = (int)ApprovalStatus.Failed;
                            break;
                        case "Bank Transfer":
                            order.Status = (int)ApprovalStatus.BankTransfer;
                            break;
                        default:
                            return false;
                    }
                    order.PaymentType = paymentType;
                    _context.Orders.Update(order);
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
        public async Task<Dictionary<int, string>> GetBankTransferOrdersForUserAsync(string userID)
        {
            try
            {
                var orderInfos = await _context.Orders
                    .Where(b => b.BuyerId == userID &&
                            b.PaymentType == 2 &&
                            b.Status == 5
                            )
                    .ToDictionaryAsync(
                        b => b.Id,
                        b => $"Sipariş {b.Id} - {b.OrderDate:dd.MM.yyyy}"
                    );

                return orderInfos ?? new Dictionary<int, string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new Dictionary<int, string>();
            }
        }
        public async Task<int> CancelOrderItemAsync(int orderId, string productCode)
        {
            try
            {
                Order? order = await _context.Orders
                                        .Include(o => o.OrderItems)
                                        .FirstOrDefaultAsync(o => o.Id == orderId);
                OrderItem item = order.OrderItems.Where(p => p.ProductCode == productCode).FirstOrDefault();
                if (order == null)
                    return 0;
                if (item == null)
                    return 0;

                // failed or pending orders can be cancelled directly
                if (order.Status == 2 ||
                    order.Status == 0 ||
                    order.Status == 5)
                {
                    await ProcessRefundToDb(order, item);
                    return 1;
                }
                //If order is approved or in shipping can be refunded
                else if (order.Status == 1 ||
                    order.Status == 3)
                {
                    // Bank transfer orders cannot be cancelled
                    if (order.PaymentType == 2)
                    {
                        await ProcessRefundToDb(order, item);
                        return 4;
                    }
                    int resultInt = await _iyziPay.CancelOrderItemAsync(order, item);
                    if (resultInt == -1)
                        return 3;
                    else if (resultInt == 2)
                        return 6;
                    else if (resultInt == 3)
                        return 7;

                    await ProcessRefundToDb(order, item);
                    return 1;
                }
                //orders delivered can be refunded when orders send back
                else if (order.Status == 4)
                {
                    if (order.PaymentType == 2)
                        return 5; // Bank transfer orders refund

                    return 2;
                }

                else
                    return -1;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cancel Order Iyzico procedure failed for {OrderId}", orderId);
                return -1;
            }
        }
        public async Task<int> CancelOrderAsync(int orderId)
        {
            try
            {
                Order? order = await _context.Orders
                                        .Include(o => o.OrderItems)
                                        .FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null)
                    return 0;

                // failed or pending orders can be cancelled directly
                if (order.Status == 2 ||
                    order.Status == 0 ||
                    order.Status == 5)
                {
                    order.Status = 6; // Set status to Cancelled
                    order.CanceledAt = DateTime.Now;
                    await _inventoryService.UpdateInventoryAsync(order, 1); // Add stock
                    _context.Orders.Update(order);
                    await _context.SaveChangesAsync();
                    return 1;
                }
                //If order is approved or in shipping can be refunded
                else if (order.Status == 1 ||
                    order.Status == 3)
                {
                    if (order.PaymentType == 2)
                    {
                        order.Status = 6; // Set status to Cancelled
                        order.CanceledAt = DateTime.Now;
                        await _inventoryService.UpdateInventoryAsync(order, 1); // Add stock
                        _context.Orders.Update(order);
                        await _context.SaveChangesAsync();
                        return 4;
                    } // Bank transfer orders cannot be cancelled
                    bool result = await _iyziPay.CancelOrderAsync(order);
                    if (!result)
                        return 3;
                    await _inventoryService.UpdateInventoryAsync(order, 1); // Add stock
                    return 1;
                }
                //orders delivered can be refunded when orders send back
                else if (order.Status == 4)
                {
                    if (order.PaymentType == 2)
                        return 5; // Bank transfer orders refund

                    return 2;
                }

                else
                    return -1;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cancel Order Iyzico procedure failed for {OrderId}", orderId);
                return -1;
            }
        }
        private async Task ProcessRefundToDb(Order order, OrderItem item)
        {
            try
            {
                item.IsRefunded = true;
                item.RefundDate = DateTime.UtcNow;
                bool allRefunded = false;
                foreach (var orderItem in order.OrderItems)
                {
                    if (orderItem.IsRefunded == false)
                    {
                        allRefunded = false;
                        break;
                    }
                }
                if (allRefunded)
                {
                    order.Status = 6; // Set status to Cancelled
                    order.CanceledAt = DateTime.UtcNow;
                    await _inventoryService.UpdateInventoryAsync(order, 1); // Add stock
                }
                else
                    await _inventoryService.UpdateInventoryItemAsync(item.ProductCode, item.Units, 1); // Add stock
                _context.Orders.Update(order);
                _context.OrderItems.Update(item);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cancel Order No Payment procedure failed for {OrderId}", order.Id);
            }
        }
        public async Task<bool> UpdateAddressAsync(Address address)
        {
            try
            {
                var existingAddress = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == address.Id && a.UserId == address.UserId);
                //if existing address set as default is different update all db
                if (address.SetAsDefault == true &&
                    existingAddress.SetAsDefault == false)
                {
                    await _context.Addresses
                        .Where(a => a.UserId == address.UserId && 
                                    a.SetAsDefault &&
                                    a.Id != address.Id)
                        .ForEachAsync(a => a.SetAsDefault = false);
                }
                existingAddress.SetAsDefault = address.SetAsDefault;
                existingAddress.FirstName = address.FirstName;
                existingAddress.LastName = address.LastName;
                existingAddress.AddressDetailed = address.AddressDetailed;
                existingAddress.State = address.State;
                existingAddress.City = address.City;
                existingAddress.CorporateName = address.CorporateName;
                existingAddress.VATnumber = address.VATnumber;
                existingAddress.VATstate = address.VATstate;
                existingAddress.Phone = address.Phone;
                existingAddress.EmailAddress = address.EmailAddress;
                existingAddress.IsBilling = address.IsBilling;
                existingAddress.IsBillingSame = address.IsBillingSame;
                existingAddress.IsCorporate = address.IsCorporate;
                existingAddress.ZipCode = address.ZipCode;
                _context.Update(existingAddress);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return false;
            }
        }
        public async Task<bool> DeleteAddressAsync(int addressId)
        {
            try
            {
                var existingAddress = await _context.Addresses
                    .FirstOrDefaultAsync(a => a.Id == addressId);
                //if existing address set as default is different update all db
                if (existingAddress == null)
                    return false;
                _context.Remove(existingAddress);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return false;
            }
        }
        public async Task<float> GetDesiAsync(string productCode)
        {
            if (string.IsNullOrEmpty(productCode))
                return 0;
            try
            {
                //check if product is bundled
                var baseProductCode = await _context.ProductVariants
                         .Where(p => p.ProductCode == productCode)
                         .Select(p => p.BaseProduct)
                         .FirstOrDefaultAsync();
                if (string.IsNullOrEmpty(baseProductCode))
                    return 0;
                var product = await _context.Products
                    .Where(p => p.ProductCode == baseProductCode)
                    .FirstOrDefaultAsync();
                if (product == null)
                    return 0;
                if (!Single.TryParse(product.Width.ToString(), out float length))
                    length = 1;
                if (!Single.TryParse(product.Height.ToString(), out float height))
                    height = 1;
                if (!Single.TryParse(product.Depth.ToString(), out float depth))
                    depth = 1;

                float desi = length * height * depth / 3000f;
                string quantityCode = productCode.Substring(14, 3);
                string? quantityStr = await _context.VariantAttributes
                    .Where(v => v.VariantCode == "001" &&
                                v.VariantAttributeCode == quantityCode)
                    .Select(v => v.VariantAttributeName)
                    .FirstOrDefaultAsync();
                if (!Int32.TryParse(quantityStr.Substring(0, quantityStr.Length - 5), out int quantity))
                    quantity = 1;
                desi = desi * quantity;
                return desi;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get Desi failed for {ProductCode}", productCode);
                return 0;
            }
        }
    
    }
}

