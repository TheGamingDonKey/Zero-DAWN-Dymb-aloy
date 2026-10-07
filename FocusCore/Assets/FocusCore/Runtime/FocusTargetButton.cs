using UnityEngine;
using UnityEngine.EventSystems;

namespace FocusCore
{
    public sealed class FocusTargetButton : MonoBehaviour, IPointerDownHandler, IPointerExitHandler
    {
        public FocusController focus;
        public bool dismiss;
        public void OnPointerDown(PointerEventData evt)
        {
            if (focus == null) return;
            focus.RefreshAvailability();
            if (dismiss) focus.TryDismiss(evt.pointerId); else focus.TryInspect(evt.pointerId);
        }
        public void OnPointerExit(PointerEventData evt) { if (focus != null) focus.Input.CancelSource(evt.pointerId); }
    }
}
