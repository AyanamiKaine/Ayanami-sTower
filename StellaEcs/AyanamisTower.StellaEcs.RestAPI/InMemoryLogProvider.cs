using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AyanamisTower.StellaEcs.Api
{
    /// <summary>
    /// Abstraction for retrieving and clearing logs captured in-memory.
    /// </summary>
    public interface ILogStore
    {
        /// <summary>
        /// Returns up to <paramref name="take"/> entries newer than <paramref name="afterId"/>,
        /// filtered by minimum <paramref name="minLevel"/> and optional <paramref name="categoryContains"/>.
        /// Results are ordered by Id ascending.
        /// </summary>
        IReadOnlyList<LogEntry> GetTail(int take = 200, long afterId = 0, LogLevel? minLevel = null, string? categoryContains = null);

        /// <summary>
        /// Clears the in-memory buffer.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// A single structured log event.
    /// </summary>
    public sealed class LogEntry
    {
        /// <summary>Monotonic identifier assigned when the entry is captured.</summary>
        public long Id { get; init; }
        /// <summary>UTC timestamp when the log event occurred.</summary>
        public DateTime TimestampUtc { get; init; }
        /// <summary>Log level.</summary>
        public LogLevel Level { get; init; }
        /// <summary>Logger category (usually type full name).</summary>
        public string Category { get; init; } = string.Empty;
        /// <summary>Event id value, if any.</summary>
        public int EventId { get; init; }
        /// <summary>Rendered message text.</summary>
        public string Message { get; init; } = string.Empty;
        /// <summary>Flattened exception (message + stack) if present.</summary>
        public string? Exception { get; init; }
    }

    /// <summary>
    /// A fast, bounded in-memory <see cref="ILoggerProvider"/> that stores recent logs in a ring buffer for UI consumption.
    /// </summary>
    public sealed class InMemoryLogProvider : ILoggerProvider, ILogStore
    {
        private readonly int _capacity;
        private readonly LogEntry[] _buffer;
        private int _index; // next write index
        private int _count;
        private long _nextId = 1;

        /// <summary>Create a provider with a fixed capacity ring buffer.</summary>
        public InMemoryLogProvider(int capacity = 2048)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
            _buffer = new LogEntry[capacity];
        }

        /// <summary>Create a logger for the given category.</summary>
        public ILogger CreateLogger(string categoryName) => new InMemoryLogger(this, categoryName);

        /// <summary>Dispose no-op.</summary>
        public void Dispose() { }

        private void Append(LogEntry entry)
        {
            // Single-writer assumption from logging pipeline; minimal locking for safety.
            lock (_buffer)
            {
                _buffer[_index] = entry;
                _index = (_index + 1) % _capacity;
                if (_count < _capacity) _count++;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<LogEntry> GetTail(int take = 200, long afterId = 0, LogLevel? minLevel = null, string? categoryContains = null)
        {
            if (take <= 0) return Array.Empty<LogEntry>();
            var min = minLevel ?? LogLevel.Trace;
            var contains = categoryContains;

            // Snapshot copy to avoid holding lock during filtering.
            LogEntry[] snapshot;
            int count;
            int index;
            lock (_buffer)
            {
                count = _count;
                index = _index;
                snapshot = new LogEntry[count];
                for (int i = 0; i < count; i++)
                {
                    var srcIdx = (index - count + i);
                    if (srcIdx < 0) srcIdx += _capacity;
                    snapshot[i] = _buffer[srcIdx];
                }
            }

            var result = new List<LogEntry>(Math.Min(take, count));
            foreach (var e in snapshot)
            {
                if (e is null) continue;
                if (e.Id <= afterId) continue;
                if (e.Level < min) continue;
                if (!string.IsNullOrEmpty(contains) && (e.Category?.IndexOf(contains, StringComparison.OrdinalIgnoreCase) < 0)) continue;
                result.Add(e);
                if (result.Count >= take) break;
            }
            return result;
        }

        /// <inheritdoc />
        public void Clear()
        {
            lock (_buffer)
            {
                Array.Clear(_buffer, 0, _buffer.Length);
                _count = 0;
                _index = 0;
            }
        }

        private sealed class InMemoryLogger : ILogger
        {
            private readonly InMemoryLogProvider _provider;
            private readonly string _category;

            public InMemoryLogger(InMemoryLogProvider provider, string category)
            {
                _provider = provider;
                _category = category;
            }

            public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel)) return;
                var id = Interlocked.Increment(ref _provider._nextId);
                var message = formatter(state, exception);
                var entry = new LogEntry
                {
                    Id = id,
                    TimestampUtc = DateTime.UtcNow,
                    Level = logLevel,
                    Category = _category,
                    EventId = eventId.Id,
                    Message = message,
                    Exception = exception?.ToString()
                };
                _provider.Append(entry);
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
