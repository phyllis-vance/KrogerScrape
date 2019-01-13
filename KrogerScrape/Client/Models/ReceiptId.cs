using System;
using System.Collections.Generic;

namespace KrogerScrape.Client
{
    public class ReceiptId : IEquatable<ReceiptId>
    {
        public string UserId { get; set; }
        public string DivisionNumber { get; set; }
        public string StoreNumber { get; set; }
        public string TransactionDate { get; set; }
        public string TerminalNumber { get; set; }
        public string TransactionId { get; set; }

        public override bool Equals(object obj)
        {
            return Equals(obj as ReceiptId);
        }

        public bool Equals(ReceiptId other)
        {
            return other != null &&
                   DivisionNumber == other.DivisionNumber &&
                   StoreNumber == other.StoreNumber &&
                   TransactionDate == other.TransactionDate &&
                   TerminalNumber == other.TerminalNumber &&
                   TransactionId == other.TransactionId;
        }

        public override int GetHashCode()
        {
            var hashCode = 1918860743;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(DivisionNumber);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(StoreNumber);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TransactionDate);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TerminalNumber);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(TransactionId);
            return hashCode;
        }

        public static bool operator ==(ReceiptId id1, ReceiptId id2)
        {
            if (ReferenceEquals(id1, null))
            {
                return ReferenceEquals(id2, null);
            }

            return id1.Equals(id2);
        }

        public static bool operator !=(ReceiptId id1, ReceiptId id2)
        {
            return !(id1 == id2);
        }

        public string GetUrl()
        {
            var pageUrl = "https://www.kroger.com/mypurchases/detail/" + string.Join("~", GetIdentifyingStrings());
            return pageUrl;
        }

        public IReadOnlyList<string> GetIdentifyingStrings()
        {
            return new[]
            {
                DivisionNumber,
                StoreNumber,
                TransactionDate,
                TerminalNumber,
                TransactionId,
            };
        }
    }
}
