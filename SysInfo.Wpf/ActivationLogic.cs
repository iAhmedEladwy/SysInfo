using System;
using System.Collections.Generic;
using System.Linq;

namespace CNSysInfo
{
    internal enum WindowsLicenseStatus { Unlicensed = 0, Licensed = 1, OOBGrace = 2, OOTGrace = 3, NonGenuineGrace = 4, Notification = 5, ExtendedGrace = 6, GracePeriod = 7 }
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
                    case WindowsLicenseStatus.GracePeriod: return "Activation grace period";
                    default: return "Unknown";
                }
            }
        }
    }

    internal static class ActivationLogic
    {
        public const string WindowsApplicationId = "55c92734-d682-4d71-983e-d6ec3f16059f";
        public static bool MatchesEdition(string name, string edition) => !string.IsNullOrWhiteSpace(edition) &&
            string.Equals(name?.Trim(), "Windows(R), " + edition + " edition", StringComparison.OrdinalIgnoreCase);
        public static ActivationResult SelectRows(IEnumerable<ActivationCandidate> rows, string edition)
        {
            if (string.IsNullOrWhiteSpace(edition)) return ActivationResult.Unavailable(ActivationUnavailableReason.NoInstalledRows);
            var candidates = rows.Where(row => string.Equals(row.ApplicationId, WindowsApplicationId, StringComparison.OrdinalIgnoreCase) && !row.IsAddon && row.Status >= 0 && row.Status <= 6);
            if (!string.IsNullOrWhiteSpace(edition)) candidates = candidates.Where(row => string.Equals(row.Family, edition, StringComparison.OrdinalIgnoreCase) || MatchesEdition(row.Name, edition));
            var usable = candidates.Where(row => row.HasPartialKey || row.Status > 0).ToArray();
            // Missing keys are not a reason to discard a licensed digital/subscription row.
            // Unlicensed catalog rows without an installed key are not proof of an unactivated OS.
            if (usable.Any(row => row.Status == 1)) return new ActivationResult(WindowsLicenseStatus.Licensed, ActivationUnavailableReason.None, DateTime.Now);
            var installed = usable.Where(row => row.HasPartialKey).ToArray();
            if (installed.Length > 0) usable = installed;
            var states = usable.Select(row => row.Status).Distinct().ToArray();
            return states.Length == 1 ? new ActivationResult((WindowsLicenseStatus)states[0], ActivationUnavailableReason.None, DateTime.Now)
                : ActivationResult.Unavailable(ActivationUnavailableReason.NoInstalledRows);
        }
        // The caller invokes this inside one bounded collection worker. Never launch a retry task.
        public static ActivationResult Read(Func<bool, int, ActivationResult> query, Func<int> remainingMs)
        {
            int remaining = remainingMs();
            if (remaining <= 0) return ActivationResult.Unavailable(ActivationUnavailableReason.Deadline);
            var first = query(false, Math.Min(2000, remaining));
            if (first.IsAvailable) return first;
            bool fallback = first.Reason == ActivationUnavailableReason.NoInstalledRows || first.Reason == ActivationUnavailableReason.Error;
            if (!fallback && !first.IsTransientFailure) return first;
            remaining = remainingMs();
            if (remaining <= 0) return ActivationResult.Unavailable(ActivationUnavailableReason.Deadline);
            return query(true, Math.Min(2000, remaining));
        }
    }

    internal sealed class ActivationCandidate
    {
        public string ApplicationId, Name, Family;
        public bool HasPartialKey, IsAddon;
        public int Status;
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
