using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KrogerScrape.Client;
using KrogerScrape.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace KrogerScrape.Logic
{
    public class JsonCommandLogic
    {
        private readonly EntityRepository _entityRepository;
        private readonly Deserializer _deserializer;
        private readonly IKrogerScrapeSettings _settings;

        public JsonCommandLogic(
            EntityRepository entityRepository,
            Deserializer deserializer,
            IKrogerScrapeSettings settings)
        {
            _entityRepository = entityRepository;
            _deserializer = deserializer;
            _settings = settings;
        }

        public async Task<string> ExecuteAsync(
            JsonCommandParameters parameters,
            CancellationToken token)
        {
            var responseEntities = await _entityRepository.GetResponsesAsync(
                _settings.Email,
                parameters.MinTransactionDate,
                parameters.MaxTransactionDate,
                token);

            JToken json = new JArray(responseEntities
                .Select(x => _deserializer.JObject(x.Decompress()))
                .ToArray());

            if (parameters.JsonPath != null)
            {
                var tokens = json.SelectTokens(parameters.JsonPath).ToList();
                if (tokens.Count == 1)
                {
                    json = tokens[0];
                }
                else
                {
                    json = new JArray(tokens);
                }
            }

            var serializedJson = JsonConvert.SerializeObject(
                json,
                parameters.Indented ? Formatting.Indented : Formatting.None);

            return serializedJson;
        }
    }
}
