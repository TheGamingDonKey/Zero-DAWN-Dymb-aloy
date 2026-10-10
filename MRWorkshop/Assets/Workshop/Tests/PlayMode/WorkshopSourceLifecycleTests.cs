using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Oculus.Interaction;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace MRWorkshop.Tests
{
    public sealed class WorkshopSourceLifecycleTests
    {
        GameObject root;
        WorkshopController workshop;
        WorkshopPart cell;
        GrabInteractor interactor;
        GrabInteractable interactable;
        WorkshopTestController device;

        [SetUp] public void SetUp()
        {
            root=new GameObject("WorkshopSourceLifecycleTest");
            workshop=root.AddComponent<WorkshopController>(); workshop.enabled=false;
            workshop.socket=new GameObject("Socket").transform; workshop.socket.SetParent(root.transform);
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.transform.SetParent(root.transform);
            var body=go.AddComponent<Rigidbody>(); body.isKinematic=true;
            var sdk=go.AddComponent<Grabbable>(); sdk.InjectOptionalRigidbody(body);
            interactable=go.AddComponent<GrabInteractable>(); interactable.InjectRigidbody(body); interactable.InjectOptionalPointableElement(sdk);
            cell=go.AddComponent<WorkshopPart>(); cell.workshop=workshop; cell.partId="sdk-cell"; cell.kind=PartKind.PowerCell; cell.CaptureHome();
            workshop.parts=new[]{cell}; workshop.SetAvailability(true);

            var source=GameObject.CreatePrimitive(PrimitiveType.Sphere); source.name="Controller source"; source.transform.SetParent(root.transform);
            source.GetComponent<Collider>().isTrigger=true;
            var sourceBody=source.AddComponent<Rigidbody>(); sourceBody.isKinematic=true;
            device=source.AddComponent<WorkshopTestController>();
            interactor=source.AddComponent<GrabInteractor>(); interactor.IsRootDriver=false;
            interactor.InjectAllGrabInteractor(source.AddComponent<WorkshopTestSelector>(),sourceBody);
            interactor.InjectOptionalActiveState(device);
            source.AddComponent<WorkshopInputSource>().Bind(workshop,interactor,device);
        }

        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(root); }

        void SelectWithActualSdk()
        {
            interactor.Enable(); interactor.SetComputeCandidateOverride(()=>interactable,false);
            interactor.ProcessCandidate(); interactor.Hover(); interactor.Select();
            Assert.That(interactor.State,Is.EqualTo(InteractorState.Select));
            Assert.That(cell.IsHeld,Is.True,"The SDK must actually select the part before the loss/release test.");
        }

        [UnityTest] public IEnumerator SourceDisconnectUnselectDoesNotDock()
        {
            yield return null; SelectWithActualSdk();
            device.connected=false; interactor.Preprocess();
            Assert.That(interactor.State,Is.EqualTo(InteractorState.Disabled));
            Assert.That(workshop.Available,Is.True,"The headset stays available during individual source loss.");
            yield return null;
            yield return null; // Test coroutines resume before LateUpdate; allow the deferred dock to run.
            Assert.That(cell.IsHeld,Is.False); Assert.That(workshop.State.DockedId,Is.Null,"SDK disable is cancellation, not physical release.");
        }

        [UnityTest] public IEnumerator EnabledSourceExplicitDisableDoesNotDock()
        {
            yield return null; SelectWithActualSdk(); interactor.Disable();
            Assert.That(device.IsConnected,Is.True,"A component/group disable also cancels, even with a connected device.");
            yield return null;
            yield return null;
            Assert.That(workshop.State.DockedId,Is.Null);
        }

        [UnityTest] public IEnumerator GenuineSdkUnselectStillDocksOnce()
        {
            yield return null; SelectWithActualSdk(); interactor.Unselect();
            Assert.That(interactor.State,Is.EqualTo(InteractorState.Hover));
            yield return null;
            yield return null;
            Assert.That(workshop.State.DockedId,Is.EqualTo(cell.partId)); Assert.That(cell.CompleteRelease(),Is.False);
        }

        [UnityTest] public IEnumerator TrackingSuspensionCancelsActualSdkSelection()
        {
            yield return null; SelectWithActualSdk(); workshop.SetAvailability(false);
            Assert.That(cell.GetComponent<Grabbable>().SelectingPointsCount,Is.Zero);
            Assert.That(cell.IsHeld,Is.False); Assert.That(cell.CompleteRelease(),Is.False);
        }

        [UnityTest] public IEnumerator InvalidControllerPoseAfterReleaseDoesNotDockOnRecovery()
        {
            yield return null; SelectWithActualSdk(); interactor.Unselect(); device.poseValid=false;
            yield return null; yield return null;
            Assert.That(workshop.State.DockedId,Is.Null);
            device.poseValid=true; yield return null; yield return null;
            Assert.That(workshop.State.DockedId,Is.Null,"Restored source validity must not revive the cancelled release.");
        }

        WorkshopButton MakePowerButton()
        {
            var events=new GameObject("Events",typeof(EventSystem)); events.transform.SetParent(root.transform);
            var button=new GameObject("Power",typeof(RectTransform),typeof(WorkshopButton)).GetComponent<WorkshopButton>();
            button.transform.SetParent(root.transform); button.workshop=workshop; button.action="Power"; return button;
        }

        [UnityTest] public IEnumerator QueuedUiClickFromDisabledSourceIsRejected()
        {
            yield return null; SelectWithActualSdk(); var button=MakePowerButton();
            var evt=new PointerEventData(EventSystem.current){pointerId=interactor.Identifier};
            ExecuteEvents.Execute(button.gameObject,evt,ExecuteEvents.pointerDownHandler);
            interactor.Disable();
            // The SDK canvas module dispatches the queued release later; exercise that handler boundary.
            ExecuteEvents.Execute(button.gameObject,evt,ExecuteEvents.pointerClickHandler);
            Assert.That(workshop.State.PowerRequested,Is.False);
        }

        [UnityTest] public IEnumerator GenuineUiClickFromTrackedSourceTogglesPower()
        {
            yield return null; SelectWithActualSdk(); var button=MakePowerButton();
            var evt=new PointerEventData(EventSystem.current){pointerId=interactor.Identifier};
            ExecuteEvents.Execute(button.gameObject,evt,ExecuteEvents.pointerDownHandler);
            interactor.Unselect(); ExecuteEvents.Execute(button.gameObject,evt,ExecuteEvents.pointerClickHandler);
            Assert.That(workshop.State.PowerRequested,Is.True);
        }

        [UnityTest] public IEnumerator RecoveredSourceDoesNotReviveOldUiPress()
        {
            yield return null; SelectWithActualSdk(); var button=MakePowerButton();
            var evt=new PointerEventData(EventSystem.current){pointerId=interactor.Identifier};
            ExecuteEvents.Execute(button.gameObject,evt,ExecuteEvents.pointerDownHandler);
            interactor.Disable(); interactor.Enable();
            ExecuteEvents.Execute(button.gameObject,evt,ExecuteEvents.pointerClickHandler);
            Assert.That(workshop.State.PowerRequested,Is.False);
            ExecuteEvents.Execute(button.gameObject,evt,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject,evt,ExecuteEvents.pointerClickHandler);
            Assert.That(workshop.State.PowerRequested,Is.True,"A fresh press from the restored valid source is admitted.");
        }

        [UnityTest] public IEnumerator HeadsetRecoveryDoesNotReviveOldUiPress()
        {
            yield return null; SelectWithActualSdk(); var button=MakePowerButton();
            var evt=new PointerEventData(EventSystem.current){pointerId=interactor.Identifier};
            ExecuteEvents.Execute(button.gameObject,evt,ExecuteEvents.pointerDownHandler);
            workshop.SetAvailability(false); workshop.SetAvailability(true);
            ExecuteEvents.Execute(button.gameObject,evt,ExecuteEvents.pointerClickHandler);
            Assert.That(workshop.State.PowerRequested,Is.False);
        }

        [UnityTest] public IEnumerator RecenterCancelsReleaseAndReplacesWorkbench()
        {
            yield return null; SelectWithActualSdk(); workshop.Action("Power"); interactor.Unselect();
            workshop.head=new GameObject("Head").transform; workshop.head.SetParent(root.transform);
            workshop.head.SetPositionAndRotation(new Vector3(2,1.7f,3),Quaternion.Euler(0,90,0));
            workshop.workbench=new GameObject("Workbench").transform; workshop.workbench.SetParent(root.transform);
            workshop.SendMessage("OnRecenter",SendMessageOptions.DontRequireReceiver);
            Assert.That(workshop.Available,Is.False,"Recenter must suspend and cancel before placing in the new tracking space.");
            Assert.That(workshop.State.PowerRequested,Is.False); Assert.That(cell.CompleteRelease(),Is.False);
            workshop.SetAvailability(true);
            workshop.SendMessage("PlaceWorkbench",SendMessageOptions.DontRequireReceiver);
            Assert.That(Vector3.Distance(workshop.workbench.position,new Vector3(3.1f,1.15f,3)),Is.LessThan(.001f));
            Assert.That(workshop.State.DockedId,Is.Null);
        }

#if UNITY_EDITOR
        [Test] public void ProductionPlatformHasUnambiguousHandAndControllerBindings()
        {
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Workshop/Prefabs/WorkshopPlatform.prefab");
            Assert.That(prefab,Is.Not.Null,"Test the actual tracked Workshop platform asset.");
            var container=new GameObject("Inactive platform fixture"); container.transform.SetParent(root.transform); container.SetActive(false);
            var platform=UnityEngine.Object.Instantiate(prefab,container.transform);
            int count=WorkshopInputSource.BindRig(workshop,platform.transform);
            var bindings=platform.GetComponentsInChildren<WorkshopInputSource>(true);
            Assert.That(count,Is.GreaterThanOrEqualTo(8)); Assert.That(bindings.Length,Is.EqualTo(count));
            int hands=0,controllers=0;
            var unique=new HashSet<MonoBehaviour>();
            foreach(var binding in bindings)
            {
                Assert.That(binding.interactorComponent,Is.InstanceOf<IInteractorView>());
                Assert.That(unique.Add(binding.interactorComponent),Is.True,"Each SDK interactor has its own binding.");
                Assert.That(binding.workshop,Is.SameAs(workshop));
                if(binding.interactorComponent is Oculus.Interaction.HandGrab.HandGrabInteractor grab)
                    Assert.That(binding.trackedDevice,Is.SameAs(new UnityEditor.SerializedObject(grab).FindProperty("_hand").objectReferenceValue),"Use the SDK hand binding even when a controller reference shares the same node.");
                if(binding.trackedDevice is IHand)hands++;
                else if(binding.trackedDevice is IController)controllers++;
                else Assert.Fail("Every generated source must bind a hand or controller.");
            }
            Assert.That(hands,Is.GreaterThanOrEqualTo(4)); Assert.That(controllers,Is.GreaterThanOrEqualTo(4));
            Assert.That(WorkshopInputSource.BindRig(workshop,platform.transform),Is.EqualTo(count));
            Assert.That(platform.GetComponentsInChildren<WorkshopInputSource>(true).Length,Is.EqualTo(count),"Repeated generation must not duplicate source adapters.");
        }
#endif
        [Test] public void MissingOrAmbiguousSourceDeviceBindingFailsExplicitly()
        {
            var platform=new GameObject("Invalid platform"); platform.transform.SetParent(root.transform); platform.SetActive(false);
            platform.AddComponent<GrabInteractor>();
            Assert.Throws<InvalidOperationException>(()=>WorkshopInputSource.BindRig(workshop,platform.transform));
            platform.AddComponent<WorkshopTestController>(); platform.AddComponent<WorkshopTestController>();
            Assert.Throws<InvalidOperationException>(()=>WorkshopInputSource.BindRig(workshop,platform.transform));
        }
    }

    // Only device data/selection signals are simulated. GrabInteractor and its lifecycle are the imported SDK.
    public sealed class WorkshopTestController : MonoBehaviour,IController,IActiveState
    {
        public bool connected=true,poseValid=true;
        public Handedness Handedness=>Handedness.Right;
        public float Scale=>1;
        public bool IsConnected=>connected;
        public bool IsPoseValid=>poseValid;
        public bool Active=>connected;
        public ControllerInput ControllerInput=>default;
        public bool TryGetPose(out Pose pose) { pose=new Pose(transform.position,transform.rotation); return poseValid; }
        public bool TryGetPointerPose(out Pose pose)=>TryGetPose(out pose);
        public bool IsButtonUsageAnyActive(ControllerButtonUsage usage)=>false;
        public bool IsButtonUsageAllActive(ControllerButtonUsage usage)=>false;
        public event Action WhenUpdated=delegate{};
    }
    public sealed class WorkshopTestSelector : MonoBehaviour,ISelector
    {
        public event Action WhenSelected=delegate{};
        public event Action WhenUnselected=delegate{};
    }
}
