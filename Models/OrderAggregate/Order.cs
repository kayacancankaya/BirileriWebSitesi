using BirileriWebSitesi.Interfaces;
using System.ComponentModel.DataAnnotations;
using static BirileriWebSitesi.Models.Enums.AprrovalStatus;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace BirileriWebSitesi.Models.OrderAggregate
{

    public class Order : IAggregateRoot
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        #pragma warning disable CS8618 // Required by Entity Framework
        public Order() { }

        public Order(string buyerId,Address shipToAddress,Address billingAddress, List<OrderItem> items, bool isInBuyRegion, bool updateUserInfo, int paymentType)
        {
            BuyerId = buyerId;
            _orderItems = items;
            ShipToAddress = shipToAddress;
            BillingAddress = billingAddress;
            IsInBuyRegion = isInBuyRegion;
            UpdateUserInfo = updateUserInfo;
            PaymentType = paymentType;
        }
        [Required]
        public string BuyerId { get; set; }
        [Required]
        public DateTimeOffset OrderDate { get; private set; } = DateTimeOffset.Now;
        public int ShipToAddressId { get; set; }
        public int BillingAddressId { get; set; }
        // DDD Patterns comment
        // Using a private collection field, better for DDD Aggregate's encapsulation
        // so OrderItems cannot be added from "outside the AggregateRoot" directly to the collection,
        // but only through the method Order.AddOrderItem() which includes behavior.
        [Required]
        private readonly List<OrderItem> _orderItems = new List<OrderItem>();
        // Using List<>.AsReadOnly() 
        // This will create a read only wrapper around the private list so is protected against "external updates".
        // It's much cheaper than .ToList() because it will not have to copy all items in a new collection. (Just one heap alloc for the wrapper instance)
        //https://msdn.microsoft.com/en-us/library/e78dcd75(v=vs.110).aspx
        [Required]
        public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();
        public decimal TotalAmount => Convert.ToDecimal(_orderItems.Sum(i => i.Units * i.UnitPrice));
        [ForeignKey(nameof(ShipToAddressId))]
        [JsonIgnore]
        public virtual Address ShipToAddress { get; private set; }
        [ForeignKey(nameof(BillingAddressId))]
        [JsonIgnore]
        public virtual Address BillingAddress { get; private set; }
        public bool IsInBuyRegion { get; private set; } = false;
        public bool UpdateUserInfo { get; set; } = false;
        public string AdditionalNotes { get; set; } = string.Empty;
        public decimal Total()
        {
            var total = 0m;
            foreach (var item in _orderItems)
            {
                total += item.UnitPrice * item.Units;
            }
            return total;
        }
        public void AddItem(string productCode, decimal unitPrice, int quantity, string productName)
        {
            if (!OrderItems.Any(p => p.ProductCode == productCode))
            {
                _orderItems.Add(new OrderItem(productCode, quantity, unitPrice, productName));
                return;
            }
            var existingItem = OrderItems.First(p => p.ProductCode == productCode);
            existingItem.AddQuantity(quantity);
        }
        public int PaymentType { get; set; } = 1;
        public int InstallmentAmount { get; set; } = 1;
        public int Status { get; set; } = (int)ApprovalStatus.Pending;

        public virtual PaymentLog? PaymentLog { get; set; } 
    }
}
