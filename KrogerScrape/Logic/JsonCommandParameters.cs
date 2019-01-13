namespace KrogerScrape.Logic
{
    public class JsonCommandParameters
    {
        public JsonCommandParameters(
            string jsonPath,
            string minTransactionDate,
            string maxTransactionDate,
            bool indented)
        {
            JsonPath = jsonPath;
            MinTransactionDate = minTransactionDate;
            MaxTransactionDate = maxTransactionDate;
            Indented = indented;
        }

        public string JsonPath { get; }
        public string MinTransactionDate { get; }
        public string MaxTransactionDate { get; }
        public bool Indented { get; }
    }
}
