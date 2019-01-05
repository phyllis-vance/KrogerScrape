namespace KrogerScrape.Support
{
    public class DequeueResult<T>
    {
        public DequeueResult(T item, bool hasItem)
        {
            Item = item;
            HasItem = hasItem;
        }

        public T Item { get; }
        public bool HasItem { get; }
    }
}
