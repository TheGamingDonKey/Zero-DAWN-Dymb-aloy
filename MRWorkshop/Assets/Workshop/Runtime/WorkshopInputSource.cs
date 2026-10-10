using System;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using UnityEngine;

namespace MRWorkshop
{
    // One explicit binding per SDK interactor; PointerEvent.Data has no source-device contract.
    public sealed class WorkshopInputSource : MonoBehaviour
    {
        public WorkshopController workshop;
        public MonoBehaviour interactorComponent,trackedDevice;
        IInteractorView subscribed;
        int registeredId;
        bool wasUsable,started;
        public int Revision { get; private set; }
        public int Identifier => ((IInteractorView)interactorComponent).Identifier;
        public bool IsUsable
        {
            get
            {
                if(!isActiveAndEnabled || interactorComponent==null || !interactorComponent.isActiveAndEnabled ||
                    !(interactorComponent is IInteractorView view) || view.State==InteractorState.Disabled ||
                    trackedDevice==null || !trackedDevice.isActiveAndEnabled)return false;
                if(trackedDevice is IHand hand)return hand.IsConnected && hand.IsTrackedDataValid && hand.IsHighConfidence;
                if(trackedDevice is IController controller)return controller.IsConnected && controller.IsPoseValid;
                return false;
            }
        }

        public void Bind(WorkshopController owner,MonoBehaviour interactor,MonoBehaviour device)
        {
            if(owner==null || !(interactor is IInteractorView) || !(device is IHand || device is IController))
                throw new ArgumentException("Workshop source requires an owner, SDK interactor and tracked hand/controller.");
            Detach(); workshop=owner; interactorComponent=interactor; trackedDevice=device;
            if(started)Attach();
        }
        void OnEnable() { if(started)Attach(); }
        void Start() { started=true; Attach(); }
        void OnDisable() { Detach(); }
        void OnDestroy() { Detach(); }
        void Attach()
        {
            // Generators serialize bindings in edit mode; SDK identifiers exist only after runtime Awake.
            if(!Application.isPlaying || !started || subscribed!=null || !isActiveAndEnabled || workshop==null || !(interactorComponent is IInteractorView view))return;
            int id=view.Identifier;
            workshop.RegisterSource(this); registeredId=id; subscribed=view;
            subscribed.WhenStateChanged+=OnStateChanged; wasUsable=IsUsable;
        }
        void Detach()
        {
            if(subscribed==null)return;
            subscribed.WhenStateChanged-=OnStateChanged;
            Invalidate(); if(workshop!=null)workshop.UnregisterSource(this,registeredId);
            subscribed=null; wasUsable=false;
        }
        void OnStateChanged(InteractorStateChangeArgs change)
        {
            // Disable emits Unselect first. Cancel the deferred release when its ensuing Disabled event arrives.
            if(change.NewState==InteractorState.Disabled) { Invalidate(); wasUsable=false; }
        }
        void Update()
        {
            bool usable=IsUsable;
            if(wasUsable && !usable)Invalidate();
            wasUsable=usable;
        }
        void Invalidate()
        {
            Revision++;
            if(workshop!=null)workshop.CancelSource(registeredId);
        }

        public static int BindRig(WorkshopController owner,Transform platform)
        {
            if(owner==null || platform==null)throw new ArgumentNullException("Workshop rig binding requires owner and platform.");
            int count=0;
            foreach(var component in platform.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if(!(component is GrabInteractor || component is HandGrabInteractor || component is RayInteractor || component is PokeInteractor))continue;
                var device=FindDevice(component,platform);
                if(device==null)throw new InvalidOperationException("No tracked device for Workshop interactor "+component.name);
                WorkshopInputSource source=null;
                foreach(var candidate in component.GetComponents<WorkshopInputSource>())
                    if(candidate.interactorComponent==component) { source=candidate; break; }
                if(source==null)source=component.gameObject.AddComponent<WorkshopInputSource>();
                source.Bind(owner,component,device); count++;
            }
            if(count==0)throw new InvalidOperationException("Workshop platform has no supported input sources.");
            return count;
        }
        static MonoBehaviour FindDevice(MonoBehaviour interactor,Transform platform)
        {
            if(interactor is HandGrabInteractor grab)
            {
                // ControllerHandGrabInteractor deliberately has both HandRef and ControllerRef.
                // Its SDK Hand binding identifies which pose source drives the grab.
                if(grab.Hand is MonoBehaviour hand)return hand;
#if UNITY_EDITOR
                var serialized=new UnityEditor.SerializedObject(grab);
                var field=serialized.FindProperty("_hand");
                if(field!=null && field.objectReferenceValue is MonoBehaviour bound && bound is IHand)return bound;
#endif
                throw new InvalidOperationException("SDK HandGrabInteractor hand binding is missing on "+interactor.name);
            }
            for(var node=interactor.transform;node!=null;node=node.parent)
            {
                MonoBehaviour device=null;
                foreach(var component in node.GetComponents<MonoBehaviour>())
                {
                    if(!(component is IHand || component is IController))continue;
                    if(device!=null)throw new InvalidOperationException("Ambiguous Workshop device binding on "+node.name);
                    device=component;
                }
                if(device!=null)return device;
                if(node==platform)break;
            }
            return null;
        }
    }
}
