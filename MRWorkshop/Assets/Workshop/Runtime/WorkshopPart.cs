using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
namespace MRWorkshop
{
    public sealed class WorkshopPart : MonoBehaviour
    {
        public WorkshopController workshop;
        public PartKind kind;
        public string partId;
        readonly HashSet<int> pointers=new HashSet<int>();
        Grabbable grabbable;
        Rigidbody body;
        Vector3 homePosition,homeScale;
        Quaternion homeRotation;
        bool homeCaptured,pendingRelease;
        int releaseFrame;
        float sizeFactor=1;
        public bool IsHeld => pointers.Count>0 || (grabbable != null && grabbable.SelectingPointsCount>0);
        public bool IsDocked => workshop != null && workshop.State.DockedId == partId;
        public float SizeFactor => sizeFactor;
        void Awake()
        {
            grabbable=GetComponent<Grabbable>(); body=GetComponent<Rigidbody>();
            if(grabbable!=null) { grabbable.InjectOptionalThrowWhenUnselected(false); grabbable.WhenPointerEventRaised+=HandlePointer; }
            if(body!=null) { body.useGravity=false; body.isKinematic=true; }
        }
        void OnDestroy() { if(grabbable!=null)grabbable.WhenPointerEventRaised-=HandlePointer; }
        void OnDisable() { CancelPending(); if(workshop!=null)workshop.State.Undock(partId); }
        void HandlePointer(PointerEvent evt)
        {
            if(evt.Type==PointerEventType.Select)BeginGrab(evt.Identifier);
            else if(evt.Type==PointerEventType.Unselect)EndGrab(evt.Identifier);
            else if(evt.Type==PointerEventType.Cancel)CancelGrab(evt.Identifier);
        }
        public void BeginGrab(int pointer)
        {
            if(workshop==null || !workshop.Available)return;
            pointers.Add(pointer); pendingRelease=false;
            workshop.State.Undock(partId); workshop.Select(this);
        }
        public void EndGrab(int pointer)
        {
            if(!pointers.Remove(pointer))return;
            if(pointers.Count==0) { pendingRelease=true; releaseFrame=Time.frameCount; }
        }
        public void CancelGrab(int pointer) { pointers.Remove(pointer); pendingRelease=false; }
        public void CancelPending() { pointers.Clear(); pendingRelease=false; }
        public void SetInteractionAvailability(bool available)
        {
            if(available && grabbable!=null)grabbable.enabled=true;
            foreach(var component in GetComponentsInChildren<MonoBehaviour>(true))
                if(component is IInteractableView)component.enabled=available;
            if(!available && grabbable!=null)grabbable.enabled=false;
            if(!available)CancelPending();
        }
        void LateUpdate()
        {
            // Finish a frame after SDK Unselect: its transformer/kinematic release must finish first.
            if(pendingRelease && Time.frameCount>releaseFrame)CompleteRelease();
        }
        public bool CompleteRelease()
        {
            if(!pendingRelease || IsHeld)return false;
            pendingRelease=false;
            return workshop!=null && workshop.TryDock(this);
        }
        public void CaptureHome()
        { homePosition=transform.position; homeRotation=transform.rotation; homeScale=transform.localScale; sizeFactor=1; homeCaptured=true; }
        public void RestoreHome()
        {
            if(!homeCaptured || IsHeld)return;
            CancelPending(); sizeFactor=1;
            transform.SetPositionAndRotation(homePosition,homeRotation); transform.localScale=homeScale;
            if(body!=null) { body.isKinematic=true; body.useGravity=false; }
        }
        public bool Resize(float factor)
        {
            if(workshop==null || !workshop.Available || IsHeld || IsDocked || !homeCaptured)return false;
            sizeFactor=WorkbenchState.ClampSize(factor); transform.localScale=homeScale*sizeFactor; return true;
        }
        public bool Rotate(float degrees)
        {
            if(workshop==null || !workshop.Available || IsHeld || IsDocked || float.IsNaN(degrees) || float.IsInfinity(degrees))return false;
            transform.Rotate(Vector3.up,degrees,Space.World); return true;
        }
    }
}
