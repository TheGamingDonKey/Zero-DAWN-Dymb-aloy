using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using Oculus.Interaction;
namespace MRWorkshop.Tests
{
    public sealed class WorkshopInteractionTests
    {
        GameObject root; WorkshopController workshop; WorkshopPart cell;
        [SetUp] public void SetUp()
        {
            root=new GameObject("WorkshopInteractionTest"); workshop=root.AddComponent<WorkshopController>(); workshop.enabled=false;
            workshop.socket=new GameObject("Socket").transform; workshop.socket.SetParent(root.transform);
            cell=GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<WorkshopPart>(); cell.transform.SetParent(root.transform);
            cell.partId="cell"; cell.kind=PartKind.PowerCell; cell.workshop=workshop; cell.CaptureHome(); workshop.parts=new[]{cell};
            // Disabling automatic XR Update invokes OnDisable and suspends availability.
            // Restore explicit simulated availability so tests exercise grab/socket rules.
            workshop.SetAvailability(true);
            Assert.That(workshop.Available,Is.True,"Test fixture must explicitly enable simulated availability.");
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(root); }
        [Test] public void LastPointerReleaseDocksExactlyOnce()
        {
            cell.BeginGrab(1); cell.BeginGrab(2); cell.EndGrab(1); Assert.That(cell.IsHeld,Is.True); Assert.That(cell.CompleteRelease(),Is.False);
            cell.EndGrab(2); Assert.That(cell.IsHeld,Is.False); Assert.That(cell.CompleteRelease(),Is.True); Assert.That(cell.IsDocked,Is.True);
            Assert.That(cell.CompleteRelease(),Is.False); cell.BeginGrab(3); Assert.That(cell.IsDocked,Is.False);
        }
        [Test] public void CancellationDoesNotDock()
        { cell.BeginGrab(1); cell.CancelGrab(1); Assert.That(cell.CompleteRelease(),Is.False); Assert.That(workshop.State.DockedId,Is.Null); }
        [Test] public void HeldOrDockedPartsRejectTransformControls()
        { cell.BeginGrab(1); Assert.That(cell.Resize(1.4f),Is.False); Assert.That(cell.Rotate(15),Is.False); cell.EndGrab(1); cell.CompleteRelease(); Assert.That(cell.Resize(1.4f),Is.False); Assert.That(cell.Rotate(15),Is.False); }
        [Test] public void ResetRefusesHeldObjectThenRestoresHome()
        { var home=cell.transform.position; cell.BeginGrab(1); cell.transform.position=Vector3.right; Assert.That(workshop.TryReset(),Is.False); Assert.That(cell.IsHeld,Is.True); Assert.That(cell.transform.position,Is.EqualTo(Vector3.right)); cell.CancelGrab(1); Assert.That(workshop.TryReset(),Is.True); Assert.That(cell.transform.position,Is.EqualTo(home)); }
        [Test] public void LossOfAvailabilityCancelsPendingDockAndPower()
        { cell.BeginGrab(1); cell.EndGrab(1); workshop.SetAvailability(false); Assert.That(cell.CompleteRelease(),Is.False); Assert.That(workshop.State.Running,Is.False); workshop.SetAvailability(true); Assert.That(workshop.State.DockedId,Is.Null); }
        [Test] public void RejectedReleaseDoesNotKeepTryingLater()
        { cell.BeginGrab(1); cell.transform.position=Vector3.right; cell.EndGrab(1); Assert.That(cell.CompleteRelease(),Is.False); cell.transform.position=Vector3.zero; Assert.That(cell.CompleteRelease(),Is.False); }
        [Test] public void FreeSizeIsClampedRelativeToAuthoredScale()
        { var authored=cell.transform.localScale; Assert.That(cell.Resize(8),Is.True); Assert.That(cell.SizeFactor,Is.EqualTo(1.5f)); Assert.That(cell.transform.localScale,Is.EqualTo(authored*1.5f)); }
    }
}
