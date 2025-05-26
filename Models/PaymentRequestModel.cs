namespace BirileriWebSitesi.Models
{
    public class PaymentRequestModel
    {
        public int OrderId { get; set; } = 0;   
        public int InstallmentAmount { get; set; } = 1;
        public int PaymentType { get; set; } = 1;
        public string? CardHolderName { get; set; }
        public string? CreditCardNumber { get; set; }
        public string? ExpMonth { get; set; }
        public string? ExpYear { get; set; }
        public string? CVV { get; set; }
        public DateTime RegistrationDate { get; set; } 
        public DateTime LastLoginDate { get; set; } 
        public string? Ip { get; set; }
        public string? City { get; set; } 
        public string? Country { get; set; } 
        public bool Force3Ds { get; set; } = false;
        public string EmailAddress { get; set; } = String.Empty;
    }
}
