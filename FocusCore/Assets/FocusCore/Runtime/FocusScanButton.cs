using UnityEngine;
using UnityEngine.EventSystems;

namespace FocusCore
{
    public sealed class FocusScanButton : MonoBehaviour, IPointerDownHandler, IPointerExitHandler
    {
        public FocusController focus;
        public void OnPointerDown(PointerEventData evt)
        {
            // Meta's PointableCanvasModule uses the interactor Identifier as pointerId.
            // Consume Select only once. SDK Unselect is never treated as physical release.
            focus.RefreshAvailability();
            focus.TryScan(evt.pointerId);
        }
        public void OnPointerExit(PointerEventData evt) { focus.Input.CancelSource(evt.pointerId); }
        void OnDisable() { if (focus != null) focus.SetAvailability(false, false, true); }
    }
}
