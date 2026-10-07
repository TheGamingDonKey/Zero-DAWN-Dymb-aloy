using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;

namespace FocusCore
{
    // Each instance belongs to ONE SDK interactor. Never find hands across the entire rig.
    public sealed class MetaScanInput : MonoBehaviour
    {
        public FocusController focus;
        public MonoBehaviour interactorComponent;
        public MonoBehaviour trackedDevice;
        public RectTransform scanRect;
        public bool poke;

        public int PointerId => ((IInteractorView)interactorComponent).Identifier;

        public void Sample()
        {
            if (focus == null || !(interactorComponent is IInteractorView)) return;
            bool tracked = isActiveAndEnabled && interactorComponent.isActiveAndEnabled;
            bool neutral = false;
            if (trackedDevice is IHand hand)
            {
                tracked &= hand.IsConnected && hand.IsTrackedDataValid && hand.IsHighConfidence;
                if (tracked && poke)
                {
                    tracked &= scanRect != null && hand.GetJointPose(HandJointId.HandIndexTip, out _);
                    if (tracked && hand.GetJointPose(HandJointId.HandIndexTip, out var tip))
                    {
                        neutral = IsPokeNeutral(tip.position);
                    }
                }
                else if (tracked) neutral = !hand.GetIndexFingerIsPinching();
            }
            else if (trackedDevice is IController controller)
            {
                tracked &= controller.IsConnected && controller.IsPoseValid;
                if (tracked && poke)
                {
                    tracked &= scanRect != null && interactorComponent is PokeInteractor;
                    if (tracked) neutral = IsPokeNeutral(((PokeInteractor)interactorComponent).Origin);
                }
                else if (tracked) neutral = !controller.IsButtonUsageAnyActive(ControllerButtonUsage.TriggerButton);
            }
            else tracked = false;
            focus.Input.ObserveSource(PointerId, tracked, neutral);
        }

        public bool IsPokeNeutral(Vector3 position)
        {
            if (scanRect == null) return false;
            var local = scanRect.InverseTransformPoint(position);
            // Canvas units are pixels; the 2.5 cm front withdrawal is a WORLD distance.
            float frontDistance = Vector3.Dot(position - scanRect.position, scanRect.forward);
            return frontDistance < -0.025f || !scanRect.rect.Contains(new Vector2(local.x,local.y));
        }

        void OnDisable()
        {
            if (focus != null && interactorComponent is IInteractorView)
                focus.Input.ObserveSource(PointerId, false, false);
        }
    }
}
