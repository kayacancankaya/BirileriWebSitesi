using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models.OrderAggregate;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static BirileriWebSitesi.Models.Enums.AprrovalStatus;
using Address = BirileriWebSitesi.Models.OrderAggregate.Address;

namespace BirileriWebSitesi.Services
{
    public class OrderService : IOrderService
    {

        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrderService> _logger;

        public OrderService (ApplicationDbContext context, ILogger<OrderService> logger,
                                UserManager<IdentityUser> user)
        {
            _context = context;
            _logger = logger;
        }

        public Task<Dictionary<Address, Address>> GetAddress(string userId)
        {
            throw new NotImplementedException();
        }
        public async Task<string> ProcessOrderAsync(Order order)
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
                }
                else
                {
                    if (string.IsNullOrEmpty(order.BillingAddress.CorporateName))
                        return "Şirket İsmi Bulunamadı.";
                    if (string.IsNullOrEmpty(order.BillingAddress.VATstate))
                        return "Vergi Dairesi Bulunamadı.";
                    if (order.BillingAddress.VATnumber == 0)
                        return "Vergi Numarası Bulunamadı.";
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

                if(order.UpdateUserInfo)
                {
                    order.ShipToAddress.SetAsDefault = true;
                    order.BillingAddress.SetAsDefault = true;
                }

                foreach(Models.OrderAggregate.OrderItem item in order.OrderItems)
                {
                    item.ProductVariant = await _context.ProductVariants.Where(p => p.ProductCode == item.ProductCode).FirstOrDefaultAsync();
                    item.ProductVariant.Product = await _context.Products.Where(p => p.ProductCode == item.ProductVariant.BaseProduct).FirstOrDefaultAsync();
                }

                Payment payment = await IyziPay(order);
                if(payment.Status == "success")
                    order.Status = (int)ApprovalStatus.Approved;
                else
                    order.Status = (int)ApprovalStatus.Failed;

                _context.Add(order);

                await _context.SaveChangesAsync();

                return order.Status.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return "Sipariş Kaydedilirken Sistemsel Hata Oluştu, Lütfen Tekrar Deneyiniz.";
            }
        }
        private async Task<Payment> IyziPay(Order order)
        {
            try
            {
                Options options = new Options();
                options.ApiKey = "sandbox-bIx3IhgRc3bjqFAisIx1x56q6M9cf3X8";
                options.SecretKey = "sandbox-TrRZHZlnbS3Z8Mc8Q28K6ito1Cdk1vLP";
                options.BaseUrl = "https://sandbox-api.iyzipay.com";

                CreatePaymentRequest request = new CreatePaymentRequest();
                request.Locale = Locale.TR.ToString();
                request.ConversationId = "123456789";
                request.Price = order.TotalAmount.ToString();
                request.PaidPrice = order.TotalAmount.ToString(); 
                request.Currency = Currency.TRY.ToString();
                request.Installment = 1;
                request.BasketId = order.Id.ToString();
                request.PaymentChannel = PaymentChannel.WEB.ToString();
                request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

                PaymentCard paymentCard = new PaymentCard();
                paymentCard.CardHolderName = "John Doe";
                paymentCard.CardNumber = "5528790000000008";
                //5890040000000016
                paymentCard.ExpireMonth = "12";
                paymentCard.ExpireYear = "2030";
                paymentCard.Cvc = "123";
                paymentCard.RegisterCard = 0;
                request.PaymentCard = paymentCard;

                Buyer buyer = new Buyer();
                buyer.Id = order.BuyerId;
                buyer.Name = order.BillingAddress.FirstName;
                buyer.Surname = order.BillingAddress.LastName;
                buyer.GsmNumber = order.BillingAddress.Phone;
                buyer.Email = order.BillingAddress.EmailAddress;
                buyer.IdentityNumber = order.BillingAddress.VATnumber.ToString();
                buyer.LastLoginDate = "2015-10-05 12:43:35";
                buyer.RegistrationDate = "2013-04-21 15:12:09";
                buyer.RegistrationAddress = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
                buyer.Ip = "85.34.78.112";
                buyer.City = "Istanbul";
                buyer.Country = "Turkey";
                buyer.ZipCode = "34732";
                request.Buyer = buyer;

                Iyzipay.Model.Address shippingAddress = new Iyzipay.Model.Address();
                shippingAddress.ContactName = string.Format("{0} {1}", order.ShipToAddress.FirstName,order.ShipToAddress.LastName);
                shippingAddress.City = order.ShipToAddress.City;
                shippingAddress.Country = order.ShipToAddress.Country;
                shippingAddress.Description = order.ShipToAddress.AddressDetailed;
                shippingAddress.ZipCode = order.ShipToAddress.ZipCode;
                request.ShippingAddress = shippingAddress;

                Iyzipay.Model.Address billingAddress = new Iyzipay.Model.Address();
                billingAddress.ContactName = string.Format("{0} {1}", order.BillingAddress.FirstName, order.BillingAddress.LastName); ;
                billingAddress.City = order.BillingAddress.City;
                billingAddress.Country = order.BillingAddress.Country;
                billingAddress.Description = order.BillingAddress.AddressDetailed;
                billingAddress.ZipCode = order.BillingAddress.ZipCode;
                request.BillingAddress = billingAddress;

                List<BasketItem> basketItems = new List<BasketItem>();
                foreach(var item in order.OrderItems)
                {
                    BasketItem basketItem = new BasketItem();
                    basketItem.Id = item.ProductCode;
                    basketItem.Name = item.ProductVariant.ProductName;
                    basketItem.Category1 = item.ProductVariant.Product.Catalog.CatalogName;
                    basketItem.Category2 = string.Empty;
                    basketItem.ItemType = BasketItemType.PHYSICAL.ToString(); 
                    basketItem.Price = item.UnitPrice.ToString();
                    basketItems.Add(basketItem);
                }
                Payment payment = await Payment.Create(request, options);
                return payment;
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, ex.Message.ToString());
                return null;
            }
        }
    }
}
