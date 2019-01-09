using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KrogerScrape.Entities
{
    public class UserEntity
    {
        public long Id { get; set; }
        [Required]
        public string Email { get; set; }

        public List<CommandEntity> CommandEntities { get; set; }
        public List<ReceiptEntity> ReceiptEntities { get; set; }
    }
}
