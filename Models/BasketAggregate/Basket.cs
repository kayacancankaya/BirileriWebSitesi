using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BirileriWebSitesi.Models.BasketAggregate
{

    public class Basket : IAggregateRoot
    {
        [Key] 
        public string BuyerId { get; private set; }
        private readonly List<BasketItem> _items = new List<BasketItem>();
        public IReadOnlyCollection<BasketItem> Items => _items.AsReadOnly();
        [Required]
        public int TotalItems => _items.Sum(i => i.Quantity);
        [Required]
        public decimal TotalAmount => Convert.ToDecimal(_items.Sum(i => i.Quantity * i.UnitPrice));

        public Basket(string buyerId)
        {
            BuyerId = buyerId;
        }
        public void AddItem(string productCode, decimal unitPrice, int quantity, string buyerID, string productName,string imagePath)
        {
            if (!Items.Any(p => p.ProductCode == productCode))
            {
                _items.Add(new BasketItem(productCode, quantity, unitPrice,buyerID,productName,imagePath));
                return;
            }
            var existingItem = Items.First(p => p.ProductCode == productCode);
            existingItem.AddQuantity(quantity);
        }

        public void RemoveEmptyItems()
        {
            _items.RemoveAll(i => i.Quantity == 0);
        }

        public void SetNewBuyerId(string buyerId)
        {
            BuyerId = buyerId;
        }
    }
}
