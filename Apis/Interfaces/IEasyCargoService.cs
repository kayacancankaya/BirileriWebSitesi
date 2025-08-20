using BirileriWebSitesi.Apis.DTOs;

namespace BirileriWebSitesi.Apis.Interfaces
{
    public interface IEasyCargoService
    {
        public Task<IEnumerable<CityDTO>> GetCitiesAsync();
        public Task<IEnumerable<DistrictDTO>> GetDistrictsAsync(string cityId);
        public Task<IEnumerable<StreetDTO>> GetStreetsAsync(string districtId);
        public Task<IEnumerable<ShipmentCompaniesDTO>> GetShipmentCompaniesAsync();
        public Task<float> CalculateShipmentAsync(string shipmentCompany, int desiAmount); 
    }
}
