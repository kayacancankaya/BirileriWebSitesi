namespace BirileriWebSitesi.Models
{
    public class PaymentLog
    {
        public string OrderId { get; set; }
        public string PaymentId { get; set; }
        public string CardAssociation { get; set; } 
        public string CardFamily { get; set; }
        public string LastFourDigits { get; set; } 
        public int Installment { get; set; }
        public string Status { get; set; } 
        public DateTime PaidAt { get; set; }
    }

}
