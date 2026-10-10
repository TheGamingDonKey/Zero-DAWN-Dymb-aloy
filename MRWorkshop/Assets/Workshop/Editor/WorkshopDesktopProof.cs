using System;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace MRWorkshop.Editor
{
    [InitializeOnLoad]
    public static class WorkshopDesktopProof
    {
        const string Requested="MRWorkshop.DesktopProofRequested";
        static WorkshopController workshop;
        static double started;
        static int stage;
        static string folder;
        static Quaternion rotorAtPower;
        static WorkshopDesktopProof() { EditorApplication.playModeStateChanged+=StateChanged; }
        [MenuItem("Workshop/Run Finite Workshop Diagnostic (Simulated Input)")]
        public static void Run()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before running the diagnostic.");
            SessionState.SetBool(Requested,true);
            try { WorkshopSceneSetup.OpenDesktop(); }
            catch { SessionState.SetBool(Requested,false); throw; }
        }
        static void StateChanged(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Requested,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                workshop=UnityEngine.Object.FindFirstObjectByType<WorkshopController>(); stage=0; started=EditorApplication.timeSinceStartup;
                folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.artifacts/workshop/proof-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
                Directory.CreateDirectory(folder); EditorApplication.update+=Tick;
            }
            else if(state==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;
            else if(state==PlayModeStateChange.EnteredEditMode) { SessionState.SetBool(Requested,false); WorkshopBootstrap.OpenScene(); }
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying || workshop==null)return;
            double elapsed=EditorApplication.timeSinceStartup-started;
            try
            {
                var cell=workshop.parts[0]; var cube=workshop.parts[1];
                if(stage==0 && elapsed>.75)
                {
                    Require(cube.Resize(1.3f) && cube.Rotate(15),"Free part adjustment failed");
                    cube.BeginGrab(9101); Require(!cube.Resize(1.5f) && !workshop.TryReset(),"Held controls accepted");
                    cube.transform.position=workshop.socket.position; cube.EndGrab(9101);
                    Require(!cube.CompleteRelease() && workshop.State.DockedId==null,"Incompatible shape docked");
                    cell.BeginGrab(9102); cell.BeginGrab(9103); cell.transform.position=workshop.socket.position;
                    cell.EndGrab(9102); Require(!cell.CompleteRelease() && cell.IsHeld,"First pointer release docked");
                    cell.EndGrab(9103); Require(cell.CompleteRelease() && cell.IsDocked,"Cell did not dock after final release");
                    Require(!cell.CompleteRelease(),"Duplicate release docked again");
                    workshop.Action("Power"); Require(workshop.State.Running,"Connected power did not start"); rotorAtPower=workshop.rotor.localRotation;
                    stage++;
                }
                else if(stage==1 && elapsed>3)
                {
                    Require(Quaternion.Angle(rotorAtPower,workshop.rotor.localRotation)>1,"Rotor did not actually rotate");
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder,"powered-workshop.png")); stage++;
                }
                else if(stage==2 && elapsed>5)
                {
                    cell.BeginGrab(9104); Require(!cell.IsDocked && !workshop.State.Running,"Grab did not disconnect power");
                    cell.CancelGrab(9104); Require(!cell.CompleteRelease(),"Cancelled grab docked");
                    Require(workshop.TryReset(),"Released reset failed");
                    cell.BeginGrab(9105); cell.EndGrab(9105); workshop.SetAvailability(false);
                    Require(!cell.CompleteRelease() && !workshop.State.Running,"Tracking loss accepted pending docking");
                    workshop.SetAvailability(true); Require(workshop.TryReset(),"Recovery reset failed");
                    ScreenCapture.CaptureScreenshot(Path.Combine(folder,"reset-workshop.png")); stage++;
                }
                else if(stage==3 && elapsed>7)
                {
                    File.WriteAllText(Path.Combine(folder,"result.json"),JsonUtility.ToJson(new ProofResult{passed=true,stages=stage,simulatedInput=true,physicalHeadsetVerified=false,checks="free rotate/resize; held resize/reset rejection; incompatible socket; multi-pointer release; duplicate suppression; powered motion; undock; cancellation; reset; tracking interruption/recovery"},true));
                    Debug.Log("Workshop finite diagnostic passed; simulated input only. Evidence: "+folder); EditorApplication.isPlaying=false;
                }
                if(elapsed>18)throw new TimeoutException("Finite workshop diagnostic exceeded eighteen seconds");
            }
            catch(Exception e)
            {
                File.WriteAllText(Path.Combine(folder,"result.json"),JsonUtility.ToJson(new ProofResult{passed=false,stages=stage,simulatedInput=true,physicalHeadsetVerified=false,checks=e.ToString()},true));
                Debug.LogError(e); EditorApplication.isPlaying=false;
            }
        }
        static void Require(bool condition,string message) { if(!condition)throw new InvalidOperationException(message); }
        [Serializable] sealed class ProofResult { public bool passed; public int stages; public bool simulatedInput,physicalHeadsetVerified; public string checks; }
    }
}
