using BirileriWebSitesi.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace BirileriWebSitesi.Models.InquiryAggregate
{
    public class Inquiry : IAggregateRoot
    {
        [Key]
        public string BuyerId { get; private set; }
        public List<InquiryItem> Items { get; private set; } = new List<InquiryItem>();
        [Required]
        public int TotalItems { get; set; }
        [Required]
        public decimal TotalAmount { get; set; }

        public Inquiry(string buyerId)
        {
            BuyerId = buyerId;
        }
        public void AddItem(string productCode, decimal unitPrice, int quantity, string buyerID, string productName, string imagePath,string slug)
        {
            if (!Items.Any(p => p.ProductCode == productCode))
            {
                Items.Add(new InquiryItem(productCode, quantity, unitPrice, buyerID, productName, imagePath, slug));
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
