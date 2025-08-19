using BirileriWebSitesi.Apis.DTOs;

namespace BirileriWebSitesi.Apis.Interfaces
{
    public interface IEasyCargoService
    {
        public Task<IEnumerable<CityDTO>> GetCitiesAsync();
        public Task<IEnumerable<DistrictDTO>> GetDistrictsAsync(string cityId);
        public Task<IEnumerable<StreetDTO>> GetStreetsAsync(string districtId);
    }
}
