namespace KrogerScrape.Client
{
    public class SignInResponse
    {
        public int StatusCode { get; set; }
        public string StatusMessage { get; set; }
        public AuthenticationState AuthenticationState { get; set; }
    }
}
