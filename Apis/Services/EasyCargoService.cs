using BirileriWebSitesi.Apis.DTOs;
using BirileriWebSitesi.Apis.Interfaces;
using Newtonsoft.Json;
using System.Collections;

namespace BirileriWebSitesi.Apis.Services
{
    public class EasyCargoService : IEasyCargoService
    {
        private readonly HttpClient _http;
        private readonly ILogger<EasyCargoService> _logger;
        private readonly IConfiguration _configuration;
        private string  _baseUrl = "https://basitkargo.com/api/";
        private string token = string.Empty;
        public EasyCargoService(HttpClient http, ILogger<EasyCargoService> logger, IConfiguration configuration)
        {
            _http = http;
            _logger = logger;
            _configuration = configuration;
            token = _configuration.GetValue<string>("EasyCargo:Token") ?? string.Empty;
        }
        public async Task<IEnumerable<CityDTO>> GetCitiesAsync()
        {
            try
            {
                _http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                string endpoint = $"https://turkiyeapi.dev/api/v1/provinces";
                var response = await _http.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        _logger.LogWarning("Empty response when fetching cities.");
                        return Enumerable.Empty<CityDTO>();
                    }


                    return Enumerable.Empty<CityDTO>();
                }
                else
                {
                    _logger.LogError($"Failed to fetch cities: {response.ReasonPhrase}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching cities.");
                return null;
            }
        }
        public async Task<IEnumerable<DistrictDTO>> GetDistrictsAsync(string cityId)
        {
            try
            {
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                string endpoint = $"{_baseUrl}country/90/city/{cityId}/districts";
                var response = await _http.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var districts = await response.Content.ReadFromJsonAsync<IEnumerable<DistrictDTO>>();
                    return districts ;
                }
                else
                {
                    _logger.LogError($"Failed to fetch districts for city {cityId}: {response.ReasonPhrase}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching districts.");
                return null;
            }
        }
        public async Task<IEnumerable<StreetDTO>> GetStreetsAsync(string districtId)
        {
            try
            {
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                string endpoint = $"{_baseUrl}country/90/district/{districtId}/streets";
                var response = await _http.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var streets = await response.Content.ReadFromJsonAsync<IEnumerable<StreetDTO>>();
                    return streets;
                }
                else
                {
                    _logger.LogError($"Failed to fetch streets for district {districtId}: {response.ReasonPhrase}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching streets.");
                return null;
            }
        }
    }
}
