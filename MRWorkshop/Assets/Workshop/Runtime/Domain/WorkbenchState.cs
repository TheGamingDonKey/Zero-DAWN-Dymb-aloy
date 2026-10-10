using System;
namespace MRWorkshop
{
    public enum PartKind { PowerCell, Cube, Orb }
    public sealed class WorkbenchState
    {
        public string DockedId { get; private set; }
        public bool PowerRequested { get; private set; }
        public bool Running => PowerRequested && DockedId != null;
        public bool TryDock(string id, PartKind kind, bool held, float distance)
        {
            if (string.IsNullOrWhiteSpace(id) || kind != PartKind.PowerCell || held || DockedId != null ||
                float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0 || distance > .14f) return false;
            DockedId=id; return true;
        }
        public bool Undock(string id)
        {
            if (DockedId == null || DockedId != id) return false;
            DockedId=null; return true;
        }
        public void TogglePower() => PowerRequested=!PowerRequested;
        public void Reset() { DockedId=null; PowerRequested=false; }
        public static float ClampSize(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return 1;
            return Math.Max(.75f,Math.Min(1.5f,value));
        }
    }
}
