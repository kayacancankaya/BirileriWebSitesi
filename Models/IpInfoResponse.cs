using Newtonsoft.Json;

namespace BirileriWebSitesi.Models
{
    public class IpInfoResponse
    {
        [JsonProperty("ip")]
        public string? Ip { get; set; }

        [JsonProperty("city")]
        public string? City { get; set; }

        [JsonProperty("region")]
        public string? Region { get; set; }

        [JsonProperty("country")]
        public string? Country { get; set; }

        [JsonProperty("loc")]
        public string? Location { get; set; }

        [JsonProperty("org")]
        public string? Organization { get; set; }

        [JsonProperty("postal")]
        public string? Postal { get; set; }

        [JsonProperty("timezone")]
        public string? Timezone { get; set; }

        [JsonProperty("readme")]
        public string? Readme { get; set; }
    }
}
