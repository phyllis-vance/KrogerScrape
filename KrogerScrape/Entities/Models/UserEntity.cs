using System.Collections.Generic;

namespace KrogerScrape.Entities
{
    public class UserEntity
    {
        public long Id { get; set; }
        public string Email { get; set; }

        public List<CommandEntity> CommandEntities { get; set; }
        public List<ReceiptIdEntity> ReceiptIdEntities { get; set; }
    }
}
