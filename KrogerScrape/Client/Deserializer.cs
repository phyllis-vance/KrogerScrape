using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KrogerScrape.Client
{
    public class Deserializer
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            DateTimeZoneHandling = DateTimeZoneHandling.Unspecified,
            DateParseHandling = DateParseHandling.DateTime,
            MissingMemberHandling = MissingMemberHandling.Error,
        };

        public AuthenticationState AuthenticationState(string json)
        {
            return Deserialize<AuthenticationState>(json);
        }

        public SignInResponse SignInResponse(string json)
        {
            return Deserialize<SignInResponse>(json);
        }

        public List<Receipt> ReceiptSummaries(string json)
        {
            return Deserialize<List<Receipt>>(json);
        }

        public Receipt Receipt(string json)
        {
            return Deserialize<Receipt>(json);
        }

        public ErrorResponse ErrorResponse(string json)
        {
            return Deserialize<ErrorResponse>(json);
        }

        public JObject JObject(string json)
        {
            return Deserialize<JObject>(json);
        }

        private T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, JsonSerializerSettings);
        }
    }
}
