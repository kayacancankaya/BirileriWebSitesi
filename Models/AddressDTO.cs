namespace BirileriWebSitesi.Models
{
    public class AddressDTO
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? CorporateName { get; set; }
        public string? EmailAddress { get; set; }
        public string? Phone { get; set; }
        public string AddressDetailed { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;

        public string ZipCode { get; set; } = string.Empty;
        public bool IsBilling { get; set; }
        public bool? IsBillingSame { get; set; }
        public bool? IsCorporate { get; set; }
        public string Country { get; set; } = string.Empty;

        public string? VATnumber { get; set; }
        public string? VATstate { get; set; }
    }
}
