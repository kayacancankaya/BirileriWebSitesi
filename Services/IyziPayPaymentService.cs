using BirileriWebSitesi.Interfaces;
using BirileriWebSitesi.Models;
using BirileriWebSitesi.Models.OrderAggregate;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace BirileriWebSitesi.Services
{
    public class IyziPayPaymentService : IIyzipayPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<IyziPayPaymentService> _logger;
        private readonly IOptions<IyzipayOptions> _iyzipayOptions;
        public IyziPayPaymentService(IConfiguration configuration, 
                                     ILogger<IyziPayPaymentService> logger,
                                     IOptions<IyzipayOptions> iyzipayOptions)
        {
            _configuration = configuration;
            _logger = logger;
            _iyzipayOptions = iyzipayOptions;
            
        }
        public async Task<string> IyziPayCreate3dsReqAsync(Order order, PaymentRequestModel model)
        {
            try
            {
                Iyzipay.Options options = await GetIyzipayOptionsAsync();

                CreatePaymentRequest request = new CreatePaymentRequest();
                request.Locale = Locale.TR.ToString();
                request.ConversationId = Guid.NewGuid().ToString();
                request.Price = order.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture);
                request.PaidPrice = order.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture);
                request.Currency = Currency.TRY.ToString();
                request.Installment = model.InstallmentAmount;
                request.BasketId = model.OrderId.ToString();
                request.PaymentChannel = PaymentChannel.WEB.ToString();
                request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

                PaymentCard paymentCard = new PaymentCard();
                paymentCard.CardHolderName = model.CardHolderName;
                paymentCard.CardNumber = model.CreditCardNumber;
                //Card number: 5890040000000016

                //Expire Month: 12

                //Expire Year: 2030

                //CVC: 123
                paymentCard.ExpireMonth = model.ExpMonth;
                paymentCard.ExpireYear = model.ExpYear;
                paymentCard.Cvc = model.CVV;
                paymentCard.RegisterCard = 0;
                request.PaymentCard = paymentCard;

                Buyer buyer = new Buyer();
                buyer.Id = order.BuyerId;
                if (order.BillingAddress.IsCorporate)
                {
                    buyer.Name = order.BillingAddress.CorporateName;
                    buyer.Surname = order.BillingAddress.VATstate;
                }
                else
                {
                    buyer.Name = order.BillingAddress.FirstName;
                    buyer.Surname = order.BillingAddress.LastName;
                }
                buyer.GsmNumber = order.BillingAddress.Phone;
                buyer.Email = order.BillingAddress.EmailAddress;
                buyer.IdentityNumber = order.BillingAddress.VATnumber.ToString() == string.Empty ? "0000000000000" : order.BillingAddress.VATnumber.ToString();
                buyer.LastLoginDate = model.LastLoginDate.ToString("yyyy-MM-dd HH:mm:ss");
                buyer.RegistrationDate = model.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss");
                buyer.RegistrationAddress = order.BillingAddress.AddressDetailed;
                buyer.Ip = model.Ip;
                buyer.City = model.City;
                buyer.Country = model.Country;
                buyer.ZipCode = order.BillingAddress.ZipCode;
                request.Buyer = buyer;

                Iyzipay.Model.Address shippingAddress = new Iyzipay.Model.Address();
                shippingAddress.ContactName = string.Format("{0} {1}", order.ShipToAddress.FirstName, order.ShipToAddress.LastName);
                shippingAddress.City = order.ShipToAddress.City;
                shippingAddress.Country = order.ShipToAddress.Country;
                shippingAddress.Description = order.ShipToAddress.AddressDetailed;
                shippingAddress.ZipCode = order.ShipToAddress.ZipCode;
                request.ShippingAddress = shippingAddress;

                Iyzipay.Model.Address billingAddress = new Iyzipay.Model.Address();
                if (order.BillingAddress.IsCorporate)
                    billingAddress.ContactName = order.BillingAddress.CorporateName;
                else
                    billingAddress.ContactName = order.BillingAddress.FirstName + " " + order.BillingAddress.LastName;

                billingAddress.City = order.BillingAddress.City;
                billingAddress.Country = order.BillingAddress.Country;
                billingAddress.Description = order.BillingAddress.AddressDetailed;
                billingAddress.ZipCode = order.BillingAddress.ZipCode;
                request.BillingAddress = billingAddress;

                List<Iyzipay.Model.BasketItem> basketItems = new List<BasketItem>();
                foreach (var item in order.OrderItems)
                {
                    Iyzipay.Model.BasketItem basketItem = new BasketItem();
                    basketItem.Id = item.ProductCode;
                    basketItem.Name = item.ProductVariant.ProductName;
                    basketItem.Category1 = item.ProductVariant.Product.Catalog.CatalogName;
                    basketItem.Category2 = string.Empty;
                    basketItem.ItemType = BasketItemType.PHYSICAL.ToString();
                    basketItem.Price = (item.UnitPrice * item.Units).ToString("0.00", CultureInfo.InvariantCulture);
                    basketItems.Add(basketItem);
                }

                request.BasketItems = basketItems;
                string htmlString = await IyziPay3ds(request, options);
                return htmlString;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return "ERROR";
            }
        }
        public async Task<Payment> IyziPayCreateReqAsync(Order order, PaymentRequestModel model)
        {
            try
            {
                Iyzipay.Options options = await GetIyzipayOptionsAsync();

                CreatePaymentRequest request = new CreatePaymentRequest();
                request.Locale = Locale.TR.ToString();
                request.ConversationId = Guid.NewGuid().ToString();
                request.Price = order.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture);
                request.PaidPrice = order.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture);
                request.Currency = Currency.TRY.ToString();
                request.Installment = model.InstallmentAmount;
                request.BasketId = model.OrderId.ToString();
                request.PaymentChannel = PaymentChannel.WEB.ToString();
                request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

                PaymentCard paymentCard = new PaymentCard();
                paymentCard.CardHolderName = model.CardHolderName;
                paymentCard.CardNumber = model.CreditCardNumber;
                //Card number: 5890040000000016

                //Expire Month: 12

                //Expire Year: 2030

                //CVC: 123
                paymentCard.ExpireMonth = model.ExpMonth;
                paymentCard.ExpireYear = model.ExpYear;
                paymentCard.Cvc = model.CVV;
                paymentCard.RegisterCard = 0;
                request.PaymentCard = paymentCard;

                Buyer buyer = new Buyer();
                buyer.Id = order.BuyerId;
                if (order.BillingAddress.IsCorporate)
                {
                    buyer.Name = order.BillingAddress.CorporateName;
                    buyer.Surname = order.BillingAddress.VATstate;
                }
                else
                {
                    buyer.Name = order.BillingAddress.FirstName;
                    buyer.Surname = order.BillingAddress.LastName;
                }
                buyer.GsmNumber = order.BillingAddress.Phone;
                buyer.Email = order.BillingAddress.EmailAddress;
                buyer.IdentityNumber = order.BillingAddress.VATnumber == string.Empty ? "00000000000" : order.BillingAddress.VATnumber;
                buyer.LastLoginDate = model.LastLoginDate.ToString("yyyy-MM-dd HH:mm:ss");
                buyer.RegistrationDate = model.RegistrationDate.ToString("yyyy-MM-dd HH:mm:ss");
                buyer.RegistrationAddress = order.BillingAddress.AddressDetailed;
                buyer.Ip = model.Ip;
                buyer.City = model.City;
                buyer.Country = model.Country;
                buyer.ZipCode = order.BillingAddress.ZipCode;
                request.Buyer = buyer;

                Iyzipay.Model.Address shippingAddress = new Iyzipay.Model.Address();
                shippingAddress.ContactName = string.Format("{0} {1}", order.ShipToAddress.FirstName, order.ShipToAddress.LastName);
                shippingAddress.City = order.ShipToAddress.City;
                shippingAddress.Country = order.ShipToAddress.Country;
                shippingAddress.Description = order.ShipToAddress.AddressDetailed;
                shippingAddress.ZipCode = order.ShipToAddress.ZipCode;
                request.ShippingAddress = shippingAddress;

                Iyzipay.Model.Address billingAddress = new Iyzipay.Model.Address();
                if (order.BillingAddress.IsCorporate)
                    billingAddress.ContactName = order.BillingAddress.CorporateName;
                else
                    billingAddress.ContactName = order.BillingAddress.FirstName + " " + order.BillingAddress.LastName;

                billingAddress.City = order.BillingAddress.City;
                billingAddress.Country = order.BillingAddress.Country;
                billingAddress.Description = order.BillingAddress.AddressDetailed;
                billingAddress.ZipCode = order.BillingAddress.ZipCode;
                request.BillingAddress = billingAddress;

                List<Iyzipay.Model.BasketItem> basketItems = new List<BasketItem>();
                foreach (var item in order.OrderItems)
                {
                    Iyzipay.Model.BasketItem basketItem = new BasketItem();
                    basketItem.Id = item.ProductCode;
                    basketItem.Name = item.ProductVariant.ProductName;
                    basketItem.Category1 = item.ProductVariant.Product.Catalog.CatalogName;
                    basketItem.Category2 = string.Empty;
                    basketItem.ItemType = BasketItemType.PHYSICAL.ToString();
                    basketItem.Price = (item.UnitPrice * item.Units).ToString("0.00", CultureInfo.InvariantCulture);
                    basketItems.Add(basketItem);
                }

                request.BasketItems = basketItems;

                Payment payment = await IyziPay(request, options);
                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return null;
            }
        }
        public async Task<ThreedsPayment> Payment3dsCallBack(string conversationID, string paymentId)
        {
            try
            {
                Iyzipay.Options options = await GetIyzipayOptionsAsync();
                var request = new CreateThreedsPaymentRequest
                {
                    Locale = Locale.TR.ToString(),
                    ConversationId = conversationID,
                    PaymentId = paymentId,
                };

                var payment = await ThreedsPayment.Create(request, options);

                return payment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return null;
            }
        }
        private async Task<string> IyziPay3ds(CreatePaymentRequest request, Iyzipay.Options options)
        {
            try
            {
                string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                bool isProduction = environment == "Production";
                if (isProduction)
                {
                    request.CallbackUrl = "https://birilerigt.com/Payment/Payment3dsCallBack";
                }
                else
                {
                    request.CallbackUrl = "https://localhost:44332/Payment/Payment3dsCallBack";
                }
                ThreedsInitialize threedsInit = await ThreedsInitialize.Create(request, options);
                if (threedsInit.Status == "success")
                {
                    // Return this HTML to the frontend and show it inside a <div> or <iframe>
                    return threedsInit.HtmlContent;
                }
                else
                    return threedsInit.ErrorMessage;

            }
            catch (Exception ex)
            {

                _logger.LogError(ex, ex.Message.ToString());
                return "ERROR";
            }
        }
        private async Task<Iyzipay.Options> GetIyzipayOptionsAsync()
        {
            try
            {
                return new Iyzipay.Options
                {
                    ApiKey = _iyzipayOptions.Value.ApiKey,
                    SecretKey = _iyzipayOptions.Value.SecretKey,
                    BaseUrl = _iyzipayOptions.Value.BaseUrl
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message.ToString());
                return null;
            }
        }
        private async Task<Payment> IyziPay(CreatePaymentRequest request, Iyzipay.Options options)
        {
            try
            {
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
