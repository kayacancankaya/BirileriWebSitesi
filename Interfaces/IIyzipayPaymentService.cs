﻿using BirileriWebSitesi.Models;
using BirileriWebSitesi.Models.OrderAggregate;
using Iyzipay.Model;
using Iyzipay.Request;

namespace BirileriWebSitesi.Interfaces
{
    public interface IIyzipayPaymentService
    {
        Task<Payment> IyziPayCreateReqAsync(Order order, PaymentRequestModel model);
        Task<string> IyziPayCreate3dsReqAsync(Order order, PaymentRequestModel model);
        Task<ThreedsPayment> Payment3dsCallBack(string conversationID, string paymentId);
        Task<Iyzipay.Options> GetIyzipayOptionsAsync();
        Task<bool> CancelOrderAsync(Order order);
        Task<int> CancelOrderItemAsync(Order order, Models.OrderAggregate.OrderItem item);
    }
}
