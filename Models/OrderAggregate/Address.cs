using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BirileriWebSitesi.Models.OrderAggregate
{
    public class Address
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string CorporateName { get; set; } = string.Empty;
        public string EmailAddress { get; set; }
        public string Phone { get; set; }
        public string AddressDetailed { get; set; }
        public string Street { get; set; } = string.Empty;

        public string City { get; set; }

        public string State { get; set; }

        public string Country { get; set; } = "TR";

        public string ZipCode { get; set; } = string.Empty;
        public bool IsBilling { get; set; }
        public bool IsBillingSame { get; set; } = false;
        public bool IsCorporate { get; set; } = false;
        public string VATnumber { get; set; } = string.Empty;
        public string VATstate { get; set; } = string.Empty;
        public bool SetAsDefault { get; set; } = false;

#pragma warning disable CS8618 // Required by Entity Framework
        public Address() { }

        public Address(string address, string street, string city, string state, string country, string zipcode, bool isBilling, bool isBillingSame, string vATnumber, string vATstate,
                        string firstName, string lastName, string email, string phone, bool isCorporate, string corporateName)
        {
            AddressDetailed = address;
            Street = street;
            City = city;
            State = state;
            Country = country;
            ZipCode = zipcode;
            IsBilling = isBilling;
            IsBillingSame = isBillingSame;
            VATnumber = vATnumber;
            VATstate = vATstate;
            FirstName = firstName;
            LastName = lastName;
            EmailAddress = email;
            Phone = phone;
            SetAsDefault = false;
            IsCorporate = isCorporate;
            CorporateName = corporateName;
        }
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
