using System.Collections.Generic;

namespace FocusCore
{
    public sealed class FocusInputRouter
    {
        private readonly FocusState state = new FocusState();
        private readonly SelectionGate gate = new SelectionGate();
        private readonly Dictionary<int, bool> trackedSources = new Dictionary<int, bool>();
        public FocusMode Mode => state.Mode;
        public double Progress => state.Progress;
        public int AcceptedScans { get; private set; }

        public void SetAvailability(bool xrReady, bool headTracked, bool paused)
        {
            state.SetAvailability(xrReady, headTracked, paused);
            if (Mode == FocusMode.Unavailable || Mode == FocusMode.Suspended)
                foreach (var source in trackedSources.Keys) gate.RequireNeutral(source);
        }

        public void ObserveSource(int id, bool tracked, bool physicallyNeutral)
        {
            if (!trackedSources.ContainsKey(id)) gate.RequireNeutral(id);
            trackedSources[id] = tracked;
            if (!tracked || Mode == FocusMode.Unavailable || Mode == FocusMode.Suspended)
                gate.RequireNeutral(id);
            else if (physicallyNeutral) gate.Release(id);
        }

        public void CancelSource(int id) { gate.RequireNeutral(id); }

        public bool TryBeginScan(int id)
        {
            if (!trackedSources.TryGetValue(id, out var tracked) || !tracked || !gate.TrySelect(id)) return false;
            // Consume a busy source's edge, so it cannot queue a scan while held.
            if (!state.TryStartScan()) return false;
            AcceptedScans++;
            return true;
        }

        public void Tick(double seconds) { state.Tick(seconds); }
    }
}
