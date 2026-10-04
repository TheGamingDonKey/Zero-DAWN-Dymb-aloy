using System;

namespace FocusCore
{
    public enum FocusMode { Unavailable, Ready, Scanning, Suspended }

    public sealed class FocusState
    {
        private readonly double scanDurationSeconds;
        private double elapsed;

        public FocusState(double scanDurationSeconds = 1.25)
        {
            if (!IsFinite(scanDurationSeconds) || scanDurationSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(scanDurationSeconds));
            this.scanDurationSeconds = scanDurationSeconds;
            Mode = FocusMode.Unavailable;
        }

        public FocusMode Mode { get; private set; }
        public double Progress { get { return Mode == FocusMode.Scanning ? elapsed / scanDurationSeconds : 0; } }

        public void SetAvailability(bool xrReady, bool headTracked, bool paused)
        {
            if (!xrReady) Reset(FocusMode.Unavailable);
            else if (!headTracked || paused) Reset(FocusMode.Suspended);
            else if (Mode == FocusMode.Unavailable || Mode == FocusMode.Suspended) Reset(FocusMode.Ready);
        }

        public bool TryStartScan()
        {
            if (Mode != FocusMode.Ready) return false;
            elapsed = 0;
            Mode = FocusMode.Scanning;
            return true;
        }

        public void Tick(double deltaSeconds)
        {
            if (!IsFinite(deltaSeconds) || deltaSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            if (Mode != FocusMode.Scanning) return;
            elapsed = Math.Min(scanDurationSeconds, elapsed + deltaSeconds);
            if (elapsed >= scanDurationSeconds) Reset(FocusMode.Ready);
        }

        private void Reset(FocusMode mode) { Mode = mode; elapsed = 0; }
        private static bool IsFinite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
