using LibGit2Sharp;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GitObjectDb.Git
{
    /// <inheritdoc/>
    internal sealed class RepositoryProvider : IRepositoryProvider, IDisposable
    {
        private static readonly TimeSpan _expirationScanFrequency = TimeSpan.FromSeconds(2);

        private readonly object _syncLock = new object();
        private readonly IDictionary<RepositoryDescription, CacheEntry> _dictionary = new Dictionary<RepositoryDescription, CacheEntry>();
        private readonly IRepositoryFactory _repositoryFactory;

        private CancellationTokenSource _scanForExpiredItemsCancellationTokenSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryProvider"/> class.
        /// </summary>
        /// <param name="repositoryFactory">The repository factory.</param>
        public RepositoryProvider(IRepositoryFactory repositoryFactory)
        {
            _repositoryFactory = repositoryFactory ?? throw new ArgumentNullException(nameof(repositoryFactory));
        }

        /// <inheritdoc/>
        public TResult Execute<TResult>(RepositoryDescription description, Func<IRepository, TResult> processor)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }
            if (processor == null)
            {
                throw new ArgumentNullException(nameof(processor));
            }

            var entry = GetEntry(description);
            try
            {
                return processor(entry.Repository);
            }
            finally
            {
                lock (_syncLock)
                {
                    entry.Counter--;
                }
                StartScanForExpiredItems();
            }
        }

        /// <inheritdoc/>
        public void Execute(RepositoryDescription description, Action<IRepository> processor)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }
            if (processor == null)
            {
                throw new ArgumentNullException(nameof(processor));
            }

            Execute(description, repository =>
            {
                processor(repository);
                return default(object);
            });
        }

        /// <inheritdoc/>
        public void Evict(RepositoryDescription description)
        {
            if (description == null)
            {
                throw new ArgumentNullException(nameof(description));
            }

            lock (_syncLock)
            {
                var kvp = _dictionary.FirstOrDefault(k => k.Key.Equals(description));
                if (kvp.Key != null)
                {
                    Evict(kvp);
                }
            }
        }

        private CacheEntry GetEntry(RepositoryDescription description)
        {
            lock (_syncLock)
            {
                if (!_dictionary.TryGetValue(description, out var result))
                {
                    _dictionary[description] = result = new CacheEntry(_repositoryFactory.CreateRepository(description));
                }
                result.Counter++;
                result.LastUsed = DateTimeOffset.UtcNow;
                return result;
            }
        }

        private void StartScanForExpiredItems()
        {
            _scanForExpiredItemsCancellationTokenSource?.Cancel();
            _scanForExpiredItemsCancellationTokenSource = new CancellationTokenSource();

            var delay = Task.Delay(_expirationScanFrequency, _scanForExpiredItemsCancellationTokenSource.Token);
            delay.ContinueWith(ScanForExpiredItems, CancellationToken.None, TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        private void ScanForExpiredItems(Task task)
        {
            lock (_syncLock)
            {
                var expiredItems = (from kvp in _dictionary
                                    where kvp.Value.Counter == 0 && kvp.Value.ShouldBeEvicted
                                    select kvp).ToList();
                foreach (var kvp in expiredItems)
                {
                    Evict(kvp);
                }
            }
        }

        private void Evict(KeyValuePair<RepositoryDescription, CacheEntry> kvp)
        {
            var collection = (ICollection<KeyValuePair<RepositoryDescription, CacheEntry>>)_dictionary;

            // By using the ICollection<KVP>.Remove overload, we additionally enforce that the exact value in the KVP is the one associated with the key.
            // this would prevent, for instance, removing an item if it got touched right after we enumerated it above.
            collection.Remove(kvp);
            kvp.Value.Evict();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _scanForExpiredItemsCancellationTokenSource?.Dispose();
        }

        private class CacheEntry
        {
            private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(1);

            public int Counter;
            public DateTimeOffset LastUsed;

            public CacheEntry(IRepository repository)
            {
                Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            }

            public IRepository Repository { get; }

            public bool ShouldBeEvicted => Counter == 0 && (DateTimeOffset.UtcNow - LastUsed) > _timeout;

            public void Evict() => Repository.Dispose();
        }
    }
}