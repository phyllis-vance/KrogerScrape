using System.Collections.Generic;

namespace KrogerScrape.Client
{
    public class UserInfo
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? HasLoyaltyCard { get; set; }
        public Store PreferredStore { get; set; }
        public List<ShoppingContext> ShoppingContexts { get; set; }
    }
}
