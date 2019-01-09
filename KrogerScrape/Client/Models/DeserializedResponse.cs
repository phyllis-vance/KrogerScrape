namespace KrogerScrape.Client
{
    public class DeserializedResponse<T>
    {
        public string RequestId { get; set; }
        public T Response { get; set; }
    }
}
