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

namespace BirileriWebSitesi.Services
{
    public class OrderService : IOrderService
    {

        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public OrderService (ApplicationDbContext context, ILogger<OrderService> logger,
                                UserManager<IdentityUser> user,
                                IServiceProvider serviceProvider)
        {
            _context = context;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _logger.LogWarning("order service created");
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
                if(string.IsNullOrEmpty(order.ShipToAddress.FirstName))
                    return "Gönderilecek Adres İsim Bulunamadı.";
                if(string.IsNullOrEmpty(order.ShipToAddress.LastName))
                    return "Gönderilecek Adres Soy İsim Bulunamadı.";
                if(string.IsNullOrEmpty(order.ShipToAddress.EmailAddress))
                    return "Gönderilecek Adres Email Bulunamadı.";
                if(string.IsNullOrEmpty(order.ShipToAddress.AddressDetailed))
                    return "Gönderilecek Adres Bilgisi Bulunamadı.";
                if(string.IsNullOrEmpty(order.ShipToAddress.City))
                    return "Gönderilecek Adres Şehir Bilgisi Bulunamadı.";
                if(string.IsNullOrEmpty(order.ShipToAddress.State))
                    return "Gönderilecek Adres İlçe Bilgisi Bulunamadı.";
                if(string.IsNullOrEmpty(order.ShipToAddress.Street))
                    return "Gönderilecek Adres Mahalle Bilgisi Bulunamadı.";

                if (order.BillingAddress == null)
                    return "Fatura Adresi Bulunamadı.";
                if(!order.BillingAddress.IsCorporate)
                {
                    if (string.IsNullOrEmpty(order.BillingAddress.FirstName))
                        return "Fatura Adresi İsim Bulunamadı.";
                    if (string.IsNullOrEmpty(order.BillingAddress.LastName))
                        return "Fatura Adresi Soy İsim Bulunamadı.";
                    order.BillingAddress.CorporateName = string.Empty;
                    order.BillingAddress.VATnumber = 0;
                }
                else
                {
                    if (string.IsNullOrEmpty(order.BillingAddress.CorporateName))
                        return "Şirket İsmi Bulunamadı.";
                    if (string.IsNullOrEmpty(order.BillingAddress.VATstate))
                        return "Vergi Dairesi Bulunamadı.";
                    if (order.BillingAddress.VATnumber == 0)
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
                    //update address
                    //check if addresses exists
                //bool result = await CheckIfAddressExistsAsync(order.ShipToAddress);
                //if(result)
                //{
                //    _context.Addresses.Update(order.ShipToAddress);
                //    await _context.SaveChangesAsync();
                //}
                //else
                //{
                //    await _context.Addresses.AddAsync(order.ShipToAddress);
                //    await _context.SaveChangesAsync();
                //}
                //result = await CheckIfAddressExistsAsync(order.BillingAddress);
                //if(result)
                //{
                //    _context.Addresses.Update(order.BillingAddress);
                //    await _context.SaveChangesAsync();
                //}
                //else
                //{
                //    await _context.Addresses.AddAsync(order.BillingAddress);
                //    await _context.SaveChangesAsync();
                //}
                        
                
                foreach(Models.OrderAggregate.OrderItem item in order.OrderItems)
                {
                    item.ProductVariant = await _context.ProductVariants.Where(p => p.ProductCode == item.ProductCode).FirstOrDefaultAsync();
                    item.ProductVariant.Product = await _context.Products.Where(p => p.ProductCode == item.ProductVariant.BaseProduct)
                                                                             .Include(c=>c.Catalog)
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
               
                if(checkString != "success")
                    return "Ödeme Esnasında Hata ile Karşılaşıldı.";
                var iyzipayService = _serviceProvider.GetRequiredService<IIyzipayPaymentService>();
                string htmlResult = await iyzipayService.IyziPayCreate3dsReqAsync(order, model);
                
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

                var iyzipayService = _serviceProvider.GetRequiredService<IIyzipayPaymentService>();
                Payment payment = await iyzipayService.IyziPayCreateReqAsync(order, model);

               if (payment.Status == "success")
                   order.Status = (int)ApprovalStatus.Approved;
               else
                   order.Status = (int)ApprovalStatus.Failed;

               if (!string.IsNullOrEmpty(payment.ErrorMessage))
                   return payment.ErrorMessage.ToString();

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                return order.Status.ToString();
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
            Iyzipay.Options options = new Iyzipay.Options();
            options.ApiKey = "sandbox-bIx3IhgRc3bjqFAisIx1x56q6M9cf3X8";
            options.SecretKey = "sandbox-TrRZHZlnbS3Z8Mc8Q28K6ito1Cdk1vLP";
            options.BaseUrl = "https://sandbox-api.iyzipay.com";

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
        private async Task<Order> GetOrderAsync(int orderId)
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
               bool result = await _context.Addresses.Where(a=>a.UserId == address.UserId &&
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
        private async Task<string>CheckOrderAsync(Order order, PaymentRequestModel model)
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
                    order.BillingAddress.VATnumber = 0;
                }
                else
                {
                    if (string.IsNullOrEmpty(order.BillingAddress.CorporateName))
                        return "Şirket İsmi Bulunamadı.";
                    if (string.IsNullOrEmpty(order.BillingAddress.VATstate))
                        return "Vergi Dairesi Bulunamadı.";
                    if (order.BillingAddress.VATnumber == 0)
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
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return false;
            }
        }

        public async Task<bool> UpdateOrderStatus(int orderID, string status)
        {
            try
            {
                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderID);
                if (order != null)
                {
                    switch(status)
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
                        default:
                            return false;
                    }
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

    }
}
