using BirileriWebSitesi.Apis.DTOs;
using BirileriWebSitesi.Models.OrderAggregate;

namespace BirileriWebSitesi.Models.ViewModels
{
    public class CheckOutViewModel
    {
        public Order? Order { get; set; }
        public IEnumerable<CityDTO>? Cities { get; set; }
    }
}
