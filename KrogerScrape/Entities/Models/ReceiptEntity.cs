using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KrogerScrape.Client;

namespace KrogerScrape.Entities
{
    public class ReceiptEntity
    {
        public long Id { get; set; }
        public long UserEntityId { get; set; }
        [Required]
        public string DivisionNumber { get; set; }
        [Required]
        public string StoreNumber { get; set; }
        [Required]
        public string TransactionDate { get; set; }
        [Required]
        public string TerminalNumber { get; set; }
        [Required]
        public string TransactionId { get; set; }
        public long? ReceiptResponseEntityId { get; set; }

        public UserEntity UserEntity { get; set; }
        public List<GetReceiptEntity> GetReceiptOperationEntities { get; set; }
        public ResponseEntity ReceiptResponseEntity { get; set; }

        public ReceiptId GetReceiptId()
        {
            return new ReceiptId
            {
                DivisionNumber = DivisionNumber,
                StoreNumber = StoreNumber,
                TerminalNumber = TerminalNumber,
                TransactionDate = TransactionDate,
                TransactionId = TransactionId,
            };
        }
    }
}
