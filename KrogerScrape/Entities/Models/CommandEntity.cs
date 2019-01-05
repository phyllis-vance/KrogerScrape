using KrogerScrape.Client;

namespace KrogerScrape.Entities
{
    public class CommandEntity : OperationEntity
    {
        public CommandEntity()
        {
            Type = OperationType.Command;
        }

        public long UserEntityId { get; set; }
        public UserEntity UserEntity { get; set; }
    }
}
