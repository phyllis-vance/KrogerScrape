using KrogerScrape.Client;

namespace KrogerScrape.Entities
{
    public class SignInEntity : OperationEntity
    {
        public SignInEntity()
        {
            Type = OperationType.SignIn;
        }
    }
}
