using BirileriWebSitesi.Apis.DTOs;
using BirileriWebSitesi.Apis.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.IdentityModel.Tokens.Jwt;

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
                //_http.DefaultRequestHeaders.Authorization =
                    //new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                string endpoint = $"https://turkiyeapi.dev/api/v1/provinces";
                var response = await _http.GetAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    
                    if (string.IsNullOrWhiteSpace(content))
                    {
                        _logger.LogWarning("Empty response when fetching cities.");
                        return Enumerable.Empty<CityDTO>();
                    };
                    var jObj = JObject.Parse(content);
                    var cities = jObj["data"]
                        .Select(c => new CityDTO
                        {
                            Id = (int)c["id"],
                            Name = (string)c["name"] ?? string.Empty
                        })
                        .ToList();

                    return cities;
                }
                else
                {
                    _logger.LogError($"Failed to fetch cities: {response.ReasonPhrase}");
                    return Enumerable.Empty<CityDTO>(); ;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching cities.");
                return Enumerable.Empty<CityDTO>(); ;
            }
        }
        public async Task<IEnumerable<DistrictDTO>> GetDistrictsAsync(string cityId)
        {
            try
            {
                int id = Convert.ToInt32(cityId);
                //_http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                string endpoint = $"https://turkiyeapi.dev/api/v1/districts?provinceId={cityId}";
                var response = await _http.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        _logger.LogWarning("Empty response when fetching districts.");
                        return Enumerable.Empty<DistrictDTO>();
                    }
                    ;
                    var jObj = JObject.Parse(content);
                    var districts = jObj["data"]
                        .Select(c => new DistrictDTO
                        {
                            Id = (int)c["id"],
                            Name = (string)c["name"] ?? string.Empty
                        })
                        .ToList();

                    return districts;
                }
                else
                {
                    _logger.LogError($"Failed to fetch cities: {response.ReasonPhrase}");
                    return Enumerable.Empty<DistrictDTO>(); 
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching districts.");
                return Enumerable.Empty<DistrictDTO>(); 
            }
        }
        public async Task<IEnumerable<StreetDTO>> GetStreetsAsync(string districtId)
        {
            try
            {
                //_http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                string endpoint = $"https://turkiyeapi.dev/api/v1/neighborhoods?districtId={districtId}";
                var response = await _http.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        _logger.LogWarning("Empty response when fetching streets.");
                        return Enumerable.Empty<StreetDTO>();
                    }
                    ;
                    var jObj = JObject.Parse(content);
                    var streets = jObj["data"]
                        .Select(c => new StreetDTO
                        {
                            Id = (int)c["id"],
                            Name = (string)c["name"] ?? string.Empty
                        })
                        .ToList();

                    return streets;
                }

                else
                {
                    _logger.LogError($"Failed to fetch cities: {response.ReasonPhrase}");
                    return Enumerable.Empty<StreetDTO>(); 
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching streets.");
                return Enumerable.Empty<StreetDTO>(); 
            }
        }
        public async Task<IEnumerable<ShipmentCompaniesDTO>> GetShipmentCompaniesAsync()
        {
            try
            {
                _http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                string endpoint = $"{_baseUrl}handlers";
                var response = await _http.GetAsync(endpoint);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        _logger.LogWarning("Empty response when fetching companies.");
                        return Enumerable.Empty<ShipmentCompaniesDTO>();
                    }
                    ;
                    var companies = JsonConvert.DeserializeObject<IEnumerable<ShipmentCompaniesDTO>>(content);

                    return companies ?? Enumerable.Empty<ShipmentCompaniesDTO>();
                }

                else
                {
                    _logger.LogError($"Failed to fetch cities: {response.ReasonPhrase}");
                    return Enumerable.Empty<ShipmentCompaniesDTO>(); 
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching streets.");
                return Enumerable.Empty<ShipmentCompaniesDTO>(); 
            }
        }
    }
}
