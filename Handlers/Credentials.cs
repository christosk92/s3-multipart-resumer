using Newtonsoft.Json;

namespace S3ResumeTest.Handlers
{
    public class Credentials
    {
        [JsonProperty("client_id")]
        public string clientid { get; set; }

        [JsonProperty("client_secret")]
        public string clientsecret { get; set; }

        [JsonProperty("audience")]
        public string audience { get; set; }

        [JsonProperty("grant_type")]
        public string granttype { get; set; }

        [JsonProperty("password")]
        public string password { get; set; }

        [JsonProperty("username")]
        public string username { get; set; }
    }
}
