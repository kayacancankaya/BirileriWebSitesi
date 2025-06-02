using BirileriWebSitesi.Models.BasketAggregate;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BirileriWebSitesi.Models.InquiryAggregate
{
    public class InquiryItem
    {
        [ForeignKey(nameof(ProductVariant))]
        public string ProductCode { get; private set; }
        [Required]
        public decimal UnitPrice { get; private set; } = decimal.Zero;
        [Required]
        public int Quantity { get; private set; }

        [Required]
        [ForeignKey(nameof(Basket))]
        public string BuyerID { get; private set; }
        public string ProductName { get; set; }
        public string ImagePath { get; set; }
        public InquiryItem(string productCode, int quantity, decimal unitPrice, string buyerID, string productName, string imagePath)
        {
            ProductCode = productCode;
            UnitPrice = unitPrice;
            BuyerID = buyerID;
            ProductName = productName;
            ImagePath = imagePath;
            SetQuantity(quantity);

        }

        public void AddQuantity(int quantity)
        {
            Quantity += quantity;
        }


        public void SetQuantity(int quantity)
        {
            Quantity = quantity;
        }

        public virtual Inquiry Inquiry { get; set; }
        public virtual ProductVariant ProductVariant { get; set; }
    }
}
