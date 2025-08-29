using BirileriWebSitesi.Data;
using BirileriWebSitesi.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BirileriWebSitesi.Models.BasketAggregate
{

    public class Basket : IAggregateRoot
    {
        [Key] 
        public string BuyerId { get; private set; }
        public List<BasketItem> Items { get; private set; } = new List<BasketItem>();
        [Required]
        public int TotalItems { get; set; }
        [Required]
        public decimal TotalAmount { get; set; }

        public Basket(string buyerId)
        {
            BuyerId = buyerId;
        }
        public void AddItem(string productCode, decimal unitPrice, int quantity, string buyerID, string productName,string imagePath,string slug)
        {
            if (!Items.Any(p => p.ProductCode == productCode))
            {
                Items.Add(new BasketItem(productCode, quantity, unitPrice,buyerID,productName,imagePath, slug));
                return;
            }
            var existingItem = Items.First(p => p.ProductCode == productCode);
            existingItem.AddQuantity(quantity);
        }

        public void RemoveEmptyItems()
        {
            Items.RemoveAll(i => i.Quantity == 0);
        }

    }
}
