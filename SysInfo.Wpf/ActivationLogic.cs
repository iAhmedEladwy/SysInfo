using System;

namespace CNSysInfo
{
    internal enum WindowsLicenseStatus { Unlicensed = 0, Licensed = 1, OOBGrace = 2, OOTGrace = 3, NonGenuineGrace = 4, Notification = 5, ExtendedGrace = 6 }
    internal enum ActivationUnavailableReason { None, Deadline, Busy, Timeout, Error, NoInstalledRows }

    internal sealed class ActivationResult
    {
        public WindowsLicenseStatus? Status { get; }
        public ActivationUnavailableReason Reason { get; }
        public DateTime ObservedAt { get; }
        public bool IsAvailable => Status.HasValue;
        public bool IsTransientFailure => !IsAvailable && Reason == ActivationUnavailableReason.Timeout;
        public ActivationResult(WindowsLicenseStatus? status, ActivationUnavailableReason reason, DateTime observedAt)
        { Status = status; Reason = status.HasValue ? ActivationUnavailableReason.None : reason; ObservedAt = observedAt; }
        public static ActivationResult Unavailable(ActivationUnavailableReason reason) => new ActivationResult(null, reason, DateTime.Now);
        public string DisplayText
        {
            get
            {
                switch (Status)
                {
                    case WindowsLicenseStatus.Licensed: return "Activated";
                    case WindowsLicenseStatus.Unlicensed: return "Not Activated";
                    case WindowsLicenseStatus.OOBGrace: return "Initial grace period";
                    case WindowsLicenseStatus.OOTGrace: return "Additional grace period";
                    case WindowsLicenseStatus.NonGenuineGrace: return "Non-genuine grace period";
                    case WindowsLicenseStatus.Notification: return "Activation notification";
                    case WindowsLicenseStatus.ExtendedGrace: return "Extended grace period";
                    default: return "Unknown";
                }
            }
        }
    }

    internal static class ActivationLogic
    {
        // The caller invokes this inside one bounded collection worker. Never launch a retry task.
        public static ActivationResult Read(Func<bool, int, ActivationResult> query, Func<int> remainingMs)
        {
            int remaining = remainingMs();
            if (remaining <= 0) return ActivationResult.Unavailable(ActivationUnavailableReason.Deadline);
            var first = query(false, Math.Min(2000, remaining));
            if (first.IsAvailable) return first;
            bool fallback = first.Reason == ActivationUnavailableReason.NoInstalledRows;
            if (!fallback && !first.IsTransientFailure) return first;
            remaining = remainingMs();
            if (remaining <= 0) return ActivationResult.Unavailable(ActivationUnavailableReason.Deadline);
            return query(fallback, Math.Min(2000, remaining));
        }
    }

    internal sealed class ActivationHistory
    {
        private ActivationResult _lastSuccess;
        public void Observe(ActivationResult result)
        {
            if (result != null && result.IsAvailable && (_lastSuccess == null || result.ObservedAt >= _lastSuccess.ObservedAt)) _lastSuccess = result;
        }
        public string FormatCurrent(ActivationResult result)
        {
            if (result != null && result.IsAvailable) return result.DisplayText;
            return _lastSuccess == null ? "Unknown (current check unavailable)" :
                _lastSuccess.DisplayText + " (last checked " + _lastSuccess.ObservedAt.ToString("yyyy-MM-dd HH:mm:ss") + "; current check unavailable)";
        }
    }
}
