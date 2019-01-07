using System;
using Newtonsoft.Json;

namespace KrogerScrape.Settings
{
    public class KrogerScrapeSettingsFactory
    {
        private KrogerScrapeSettings _settings;

        public KrogerScrapeSettingsFactory()
        {
        }

        public void Initialize(KrogerScrapeSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _settings = Clone(settings);
        }

        public IKrogerScrapeSettings Create()
        {
            if (_settings == null)
            {
                throw new InvalidOperationException($"The {nameof(Initialize)} method must be called first.");
            }

            return _settings;
        }

        private T Clone<T>(T settings)
        {
            var json = JsonConvert.SerializeObject(settings);
            var clone = JsonConvert.DeserializeObject<T>(json);
            var clonedJson = JsonConvert.SerializeObject(clone);
            if (clonedJson != json)
            {
                throw new InvalidOperationException("The input could not be cloned.");
            }

            return clone;
        }
    }
}
