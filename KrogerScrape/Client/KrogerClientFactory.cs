using System;

namespace KrogerScrape.Client
{
    public class KrogerClientFactory
    {
        private readonly Func<KrogerClient> _create;

        public KrogerClientFactory(Func<KrogerClient> create)
        {
            _create = create;
        }

        public KrogerClient Create() => _create();
    }
}
