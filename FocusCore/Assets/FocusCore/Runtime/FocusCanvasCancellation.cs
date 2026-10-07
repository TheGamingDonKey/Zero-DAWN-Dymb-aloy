using Oculus.Interaction;
using UnityEngine;

namespace FocusCore
{
    public sealed class FocusCanvasCancellation : MonoBehaviour
    {
        public FocusController focus;
        public PointableCanvas pointable;
        void OnEnable() { if (pointable != null) pointable.WhenPointerEventRaised += OnPointer; }
        void OnDisable() { if (pointable != null) pointable.WhenPointerEventRaised -= OnPointer; }
        void OnPointer(PointerEvent evt)
        {
            if (evt.Type == PointerEventType.Cancel) focus.Input.CancelSource(evt.Identifier);
        }
    }
}
