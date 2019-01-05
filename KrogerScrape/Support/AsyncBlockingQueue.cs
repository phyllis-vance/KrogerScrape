using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace KrogerScrape.Support
{
    public class AsyncBlockingQueue<T> : IDisposable
    {
        private readonly ConcurrentQueue<T> _queue;
        private readonly SemaphoreSlim _waitSemaphore;
        private readonly CancellationTokenSource _isCompleteCts;

        public AsyncBlockingQueue()
        {
            _queue = new ConcurrentQueue<T>();
            _waitSemaphore = new SemaphoreSlim(0);
            _isCompleteCts = new CancellationTokenSource();
        }

        public void MarkAsComplete()
        {
            _isCompleteCts.Cancel();
        }

        public int Count => _queue.Count;

        public void Enqueue(T item)
        {
            _queue.Enqueue(item);
            _waitSemaphore.Release();
        }

        public void EnqueueRange(IEnumerable<T> items)
        {
            var itemCount = 0;
            foreach (var item in items)
            {
                _queue.Enqueue(item);
                itemCount++;
            }

            if (itemCount > 0)
            {
                _waitSemaphore.Release(itemCount);
            }
        }

        public async Task<DequeueResult<T>> TryDequeueAsync()
        {
            while (true)
            {
                if (!await _waitSemaphore.WaitAsync(TimeSpan.Zero))
                {
                    try
                    {
                        await _waitSemaphore.WaitAsync(_isCompleteCts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        return new DequeueResult<T>(default(T), hasItem: false);
                    }
                }

                if (_queue.TryDequeue(out var item))
                {
                    return new DequeueResult<T>(item, hasItem: true);
                }
            }
        }

        public void Dispose()
        {
            _waitSemaphore.Dispose();
        }
    }
}
