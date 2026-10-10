using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Management;
namespace MRWorkshop
{
    public sealed class WorkshopController : MonoBehaviour
    {
        public WorkshopPart[] parts;
        public Transform socket;
        public Transform rotor;
        public bool desktopMode;
        public Transform head,workbench;
        public Text status;
        public WorkshopPart Selected { get; private set; }
        public string Message { get; private set; }="Place the cell in the power socket";
        public WorkbenchState State { get; } = new WorkbenchState();
        public bool Available { get; private set; } = true;
        bool placed,paused;
        void Start()
        {
            if(parts!=null)foreach(var part in parts)if(part!=null)part.CaptureHome();
            if(!desktopMode)SetAvailability(false);
        }
        void Update()
        {
            if(!desktopMode)
            {
                var manager=XRGeneralSettings.Instance!=null?XRGeneralSettings.Instance.Manager:null;
                bool ready=!paused && manager!=null && manager.isInitializationComplete && OVRManager.isHmdPresent && OVRManager.hasVrFocus && OVRManager.IsInsightPassthroughInitialized() && !OVRManager.HasInsightPassthroughInitFailed() && OVRPlugin.GetNodePositionTracked(OVRPlugin.Node.Head) && OVRPlugin.GetNodeOrientationTracked(OVRPlugin.Node.Head);
                SetAvailability(ready);
                if(ready && !placed && head!=null && workbench!=null)
                {
                    var forward=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;
                    if(forward.sqrMagnitude<.1f)forward=Vector3.forward;
                    workbench.SetPositionAndRotation(new Vector3(head.position.x,Mathf.Clamp(head.position.y-.55f,.65f,1.3f),head.position.z)+forward*1.1f,Quaternion.LookRotation(forward));
                    foreach(var part in parts)part.CaptureHome(); placed=true;
                }
            }
            if(rotor!=null && Available && State.Running)rotor.Rotate(Vector3.up,90*Time.unscaledDeltaTime,Space.Self);
            if(status!=null)status.text=StatusText();
        }
        public string StatusText()
        {
            return (!Available?"WAITING FOR HEADSET TRACKING":State.Running?"MECHANISM RUNNING":State.DockedId!=null?"CELL CONNECTED / POWER OFF":"SOCKET EMPTY")+
                "\n"+(Selected!=null?"Selected: "+Selected.kind+" / "+Selected.SizeFactor.ToString("0.00")+"x":"Select a part to rotate or resize")+"\n"+Message;
        }
        public void Select(WorkshopPart part) { Selected=part; Message="Release near the socket to connect a power cell"; }
        public bool TryReset()
        {
            if(!Available)return false;
            if(parts!=null)foreach(var part in parts)if(part!=null && part.IsHeld) { Message="Release held parts before resetting"; return false; }
            State.Reset(); Selected=null;
            if(parts!=null)foreach(var part in parts)if(part!=null)part.RestoreHome();
            if(rotor!=null)rotor.localRotation=Quaternion.identity;
            Message="Workbench reset"; return true;
        }
        public void SetAvailability(bool available)
        {
            if(Available==available)return;
            Available=available;
            if(!available)
            {
                State.Reset(); Selected=null; placed=false;
                if(parts!=null)foreach(var part in parts)if(part!=null) { part.SetInteractionAvailability(false); part.CancelPending(); }
                Message="Tracking interrupted / pending connections cancelled";
            }
            else { if(parts!=null)foreach(var part in parts)if(part!=null) { if(!part.IsHeld)part.RestoreHome(); if(!desktopMode)part.SetInteractionAvailability(true); } Message="Ready"; }
        }
        public bool TryDock(WorkshopPart part)
        {
            if(!Available || socket==null || part==null)return false;
            if(!State.TryDock(part.partId,part.kind,part.IsHeld,Vector3.Distance(part.transform.position,socket.position)))
            { Message=State.DockedId!=null?"Socket occupied":part.kind!=PartKind.PowerCell?"This socket accepts power cells":"Release closer to the socket"; return false; }
            part.transform.SetPositionAndRotation(socket.position,socket.rotation);
            Message="Cell locked / grab to disconnect"; return true;
        }
        public void Action(string action)
        {
            if(!Available)return;
            if(action=="Power") { State.TogglePower(); Message=State.Running?"Power on":State.PowerRequested?"Power armed / connect the cell":"Power off"; }
            else if(action=="Reset")TryReset();
            else if(Selected==null)Message="Select a part first";
            else
            {
                bool success=action=="Rotate"?Selected.Rotate(15):action=="Larger"?Selected.Resize(Selected.SizeFactor+.1f):action=="Smaller"&&Selected.Resize(Selected.SizeFactor-.1f);
                Message=success?"Part adjusted":"Release and disconnect the part before adjusting it";
            }
        }
        void OnApplicationPause(bool pause) { paused=pause; if(pause)SetAvailability(false); }
        void OnDisable() { SetAvailability(false); }
    }
}
