using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CNSysInfo
{
    // A timeout abandons the result, not the native operation. The global gate
    // bounds abandoned workers even when a provider ignores WMI timeouts.
    internal sealed class CollectionSession : IDisposable
    {
        private static readonly SemaphoreSlim WorkerSlots = new SemaphoreSlim(4, 4);
        private static readonly AsyncLocal<CollectionSession?> Active = new AsyncLocal<CollectionSession?>();
        private readonly CollectionSession? _previous;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private readonly int _budgetMs;
        public List<string> Warnings { get; } = new List<string>();
        public static CollectionSession? Current => Active.Value;

        public CollectionSession(int budgetMs)
        {
            _budgetMs = budgetMs;
            _previous = Active.Value;
            Active.Value = this;
        }

        public int RemainingMilliseconds => Math.Max(0, _budgetMs - (int)_clock.ElapsedMilliseconds);

        public T Read<T>(Func<T> read, string name, T fallback, int timeoutMs = 1500, Func<string, T> unavailable = null)
        {
            int remaining = Math.Min(timeoutMs, _budgetMs - (int)_clock.ElapsedMilliseconds);
            if (remaining <= 0)
            {
                Warnings.Add(name + ": collection deadline reached");
                return unavailable != null ? unavailable("deadline") : fallback;
            }
            if (!WorkerSlots.Wait(0))
            {
                Warnings.Add(name + ": providers still busy from earlier reads");
                return unavailable != null ? unavailable("busy") : fallback;
            }
            Task<T> task;
            try
            {
                task = Task.Run(() =>
                {
                    try { return read(); }
                    finally { WorkerSlots.Release(); }
                });
            }
            catch { WorkerSlots.Release(); throw; }
            // Observe faults even when a timed-out worker completes later.
            _ = task.ContinueWith(t => { _ = t.Exception; },
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
            try
            {
                if (task.Wait(remaining))
                {
                    var value = task.Result;
                    if (value == null || (value is string text && (string.IsNullOrWhiteSpace(text) || text == "Unknown")))
                        Warnings.Add(name + ": data unavailable");
                    return value;
                }
                Warnings.Add(name + ": timed out");
                return unavailable != null ? unavailable("timeout") : fallback;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Collection read {name}: {ex}");
                Warnings.Add(name + ": read failed");
                return unavailable != null ? unavailable("error") : fallback;
            }
        }

        public void Dispose() { Active.Value = _previous; }
    }
}
