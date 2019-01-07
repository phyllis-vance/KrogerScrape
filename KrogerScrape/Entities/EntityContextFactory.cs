using System;

namespace KrogerScrape.Entities
{
    public class EntityContextFactory
    {
        private readonly Func<IEntityContext> _create;

        public EntityContextFactory(Func<IEntityContext> create)
        {
            _create = create;
        }

        public IEntityContext Create() => _create();
    }
}
