using System.Collections.Generic;

namespace KrogerScrape.Client
{
    public class AuthenticationState
    {
        public List<string> Authorities { get; set; }
        public UserInfo UserInfo { get; set; }
        public bool? Authenticated { get; set; }
    }
}
