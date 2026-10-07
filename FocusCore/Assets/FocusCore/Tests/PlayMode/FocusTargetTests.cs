using NUnit.Framework;
using UnityEngine;

namespace FocusCore.Tests
{
    public sealed class FocusTargetTests
    {
        GameObject root;
        FocusTargets targets;
        Transform head;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("AuthoredTargetTest");
            targets = root.AddComponent<FocusTargets>(); targets.enabled = false;
            head = new GameObject("SimulatedHead").transform; head.SetParent(root.transform);
            targets.head = head; targets.Initialize(); targets.Place(Vector3.zero,Vector3.forward);
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(root); }

        [Test]
        public void ScanWaveRevealsTargetsThenGazeOpensCorrectInformation()
        {
            Assert.That(targets.RevealedCount,Is.Zero);
            targets.BeginScan(Vector3.zero);
            targets.Advance(.08f);
            Assert.That(targets.RevealedCount,Is.Zero,"Wave has not reached the targets.");
            targets.Advance(.8f);
            Assert.That(targets.RevealedCount,Is.EqualTo(3));
            head.LookAt(targets.GetTargetPosition(1)); targets.UpdateGaze();
            Assert.That(targets.HoveredIndex,Is.EqualTo(1));
            Assert.That(targets.InspectHovered(),Is.True);
            Assert.That(targets.SelectedIndex,Is.EqualTo(1));
            Assert.That(targets.InformationVisible,Is.True);
            targets.Dismiss();
            Assert.That(targets.InformationVisible,Is.False);
        }

        [Test]
        public void HiddenOrBehindHeadTargetsCannotBeInspected()
        {
            head.LookAt(targets.GetTargetPosition(0)); targets.UpdateGaze();
            Assert.That(targets.InspectHovered(),Is.False);
            targets.BeginScan(Vector3.zero); targets.Advance(1);
            head.rotation = Quaternion.Euler(0,180,0); targets.UpdateGaze();
            Assert.That(targets.HoveredIndex,Is.EqualTo(-1));
            Assert.That(targets.InspectHovered(),Is.False);
        }

        [Test]
        public void ScanUsesFixedOriginAndOnlyRevealsInRange()
        {
            targets.BeginScan(Vector3.left * 8); targets.Advance(1);
            Assert.That(targets.RevealedCount,Is.Zero);
            targets.BeginScan(Vector3.zero); targets.Advance(.8f);
            Assert.That(targets.RevealedCount,Is.EqualTo(3));
            var original = targets.GetTargetPosition(0);
            head.position = Vector3.right * 5; targets.UpdateGaze();
            Assert.That(targets.GetTargetPosition(0),Is.EqualTo(original));
        }

        [Test]
        public void TrackingLossHidesTargetsAndClearsInformation()
        {
            targets.BeginScan(Vector3.zero); targets.Advance(1);
            head.LookAt(targets.GetTargetPosition(0)); targets.UpdateGaze(); targets.InspectHovered();
            targets.Hide();
            Assert.That(targets.RevealedCount,Is.Zero);
            Assert.That(targets.HoveredIndex,Is.EqualTo(-1));
            Assert.That(targets.SelectedIndex,Is.EqualTo(-1));
            Assert.That(targets.InformationVisible,Is.False);
        }

        [Test]
        public void UnselectedHoverDoesNotSurviveTrackingRecovery()
        {
            targets.BeginScan(Vector3.zero); targets.Advance(1);
            head.LookAt(targets.GetTargetPosition(0)); targets.UpdateGaze();
            var marker = root.transform.Find("AuthoredVirtualTargets").GetChild(0);
            Assert.That(marker.localScale.x,Is.GreaterThan(1));
            targets.Hide(); targets.Place(Vector3.zero,Vector3.forward);
            targets.BeginScan(Vector3.zero); targets.Advance(1);
            head.rotation = Quaternion.Euler(0,180,0); targets.UpdateGaze();
            Assert.That(targets.HoveredIndex,Is.EqualTo(-1));
            Assert.That(marker.localScale,Is.EqualTo(Vector3.one));
            var properties = new MaterialPropertyBlock(); marker.GetComponentInChildren<Renderer>().GetPropertyBlock(properties);
            Assert.That(properties.GetColor("_BaseColor").r,Is.EqualTo(.58f).Within(.001f));
        }

        [Test]
        public void ControllerSharesPhysicalNeutralGateAndRecoveryNeedsNewScan()
        {
            var focus = root.AddComponent<FocusController>(); focus.enabled = false;
            var panel = new GameObject("WorldPanel"); panel.transform.SetParent(root.transform);
            focus.head = head; focus.panel = panel.transform; focus.targets = targets;
            focus.SetAvailability(true,true,false); focus.Input.ObserveSource(42,true,true);
            Assert.That(focus.TryScan(42),Is.True);
            focus.Advance(1.3f);
            Assert.That(targets.RevealedCount,Is.EqualTo(3),"A long frame completes the reveal.");
            head.LookAt(targets.GetTargetPosition(0));
            Assert.That(focus.TryInspect(42),Is.False,"Held Scan cannot also inspect.");
            focus.Input.ObserveSource(42,true,true);
            Assert.That(focus.TryInspect(42),Is.True);
            Assert.That(targets.SelectedIndex,Is.Zero);
            Assert.That(focus.TryDismiss(42),Is.False,"Held Inspect cannot also dismiss.");
            focus.SetAvailability(true,false,false);
            Assert.That(targets.RevealedCount,Is.Zero);
            Assert.That(targets.InformationVisible,Is.False);
            focus.SetAvailability(true,true,false); focus.Input.ObserveSource(42,true,false);
            Assert.That(focus.TryInspect(42),Is.False,"Held recovery cannot reopen information.");
            focus.Input.ObserveSource(42,true,true);
            Assert.That(focus.TryInspect(42),Is.False,"Recovery requires a fresh reveal.");
        }
    }
}
