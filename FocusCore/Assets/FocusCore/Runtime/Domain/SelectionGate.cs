using System.Collections.Generic;

namespace FocusCore
{
    public sealed class SelectionGate
    {
        private readonly HashSet<int> selected = new HashSet<int>();
        private readonly HashSet<int> requiresNeutral = new HashSet<int>();

        public bool TrySelect(int sourceId)
        {
            if (requiresNeutral.Contains(sourceId)) return false;
            return selected.Add(sourceId);
        }

        // Release means physically observed neutral with valid tracking, not SDK cancellation.
        public void Release(int sourceId)
        {
            selected.Remove(sourceId);
            requiresNeutral.Remove(sourceId);
        }

        public void RequireNeutral(int sourceId)
        {
            selected.Remove(sourceId);
            requiresNeutral.Add(sourceId);
        }

        // Only for a disposed rig; restoration of the same input source must use RequireNeutral.
        public void Clear() { selected.Clear(); requiresNeutral.Clear(); }
    }
}
