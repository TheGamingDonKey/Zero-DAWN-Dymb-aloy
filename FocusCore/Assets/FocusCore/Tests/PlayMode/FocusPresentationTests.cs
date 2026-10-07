using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FocusCore.Tests
{
    public sealed class FocusPresentationTests
    {
        [Test]
        public void PanelStaysWorldFixedThenRepositionsAfterRecovery()
        {
            var root = new GameObject("PanelRecoveryTest");
            var focus = root.AddComponent<FocusController>(); focus.enabled = false;
            var head = new GameObject("Head"); var panel = new GameObject("Panel");
            focus.head = head.transform; focus.panel = panel.transform;
            focus.SetAvailability(true,true,false); var first = panel.transform.position;
            head.transform.position = Vector3.right * 2;
            focus.SetAvailability(true,true,false);
            Assert.That(panel.transform.position,Is.EqualTo(first));
            focus.SetAvailability(true,false,false); focus.SetAvailability(true,true,false);
            Assert.That(panel.transform.position,Is.EqualTo(first + Vector3.right * 2));
            Object.DestroyImmediate(root); Object.DestroyImmediate(head); Object.DestroyImmediate(panel);
        }

        [Test]
        public void PokeRequiresFrontWithdrawalOrLeavingButtonBounds()
        {
            var root = new GameObject("ScaledCanvas",typeof(RectTransform));
            var rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(200,80); rect.localScale = Vector3.one * .0008f;
            var input = root.AddComponent<MetaScanInput>(); input.scanRect = rect;
            Assert.That(input.IsPokeNeutral(new Vector3(0,0,-.005f)),Is.False);
            Assert.That(input.IsPokeNeutral(new Vector3(0,0,.05f)),Is.False,"Passing behind the button is not withdrawal.");
            Assert.That(input.IsPokeNeutral(new Vector3(0,0,-.03f)),Is.True);
            Assert.That(input.IsPokeNeutral(new Vector3(.1f,0,0)),Is.True);
            Object.DestroyImmediate(root);
        }

        [UnityTest]
        public IEnumerator OriginStaysFixedAndBusyRequestDoesNotDuplicateAudio()
        {
            var root = new GameObject("FocusPresentationTest");
            var focus = root.AddComponent<FocusController>();
            focus.enabled = false; // Drive availability explicitly; no mock claims about physical XR.
            var head = new GameObject("TestHead"); focus.head = head.transform;
            var pulse = new GameObject("TestPulse"); focus.visuals = pulse.AddComponent<FocusVisuals>();
            focus.audioSource = root.AddComponent<AudioSource>();
            focus.chirp = AudioClip.Create("TestChirp",1000,1,44100,false);
            focus.SetAvailability(true,true,false);
            focus.Input.ObserveSource(17,true,true);
            head.transform.position = new Vector3(1,2,3);
            Assert.That(focus.TryScan(17),Is.True);
            focus.Input.ObserveSource(19,true,true);
            Assert.That(focus.TryScan(19),Is.False);
            Assert.That(focus.PlayedChirps,Is.EqualTo(1));
            head.transform.position = new Vector3(9,9,9);
            focus.Advance(0.5f);
            Assert.That(focus.visuals.Origin,Is.EqualTo(new Vector3(1,2,3)));
            Assert.That(pulse.transform.position,Is.EqualTo(focus.visuals.Origin));
            Assert.That(focus.visuals.Active,Is.True);
            focus.Advance(0.75f);
            Assert.That(focus.visuals.Active,Is.False);
            Assert.That(focus.Input.Mode,Is.EqualTo(FocusMode.Ready));
            Object.Destroy(focus.chirp); Object.Destroy(root); Object.Destroy(head); Object.Destroy(pulse);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HeadTrackingLossStopsPresentationAndHeldRecoveryDoesNotReplay()
        {
            var root = new GameObject("FocusRecoveryTest");
            var focus = root.AddComponent<FocusController>(); focus.enabled = false;
            focus.head = root.transform;
            var pulse = new GameObject("TestPulse"); focus.visuals = pulse.AddComponent<FocusVisuals>();
            focus.SetAvailability(true,true,false); focus.Input.ObserveSource(17,true,true);
            Assert.That(focus.TryScan(17),Is.True);
            focus.SetAvailability(true,false,false);
            Assert.That(focus.visuals.Active,Is.False);
            focus.SetAvailability(true,true,false); focus.Input.ObserveSource(17,true,false);
            Assert.That(focus.TryScan(17),Is.False);
            Assert.That(focus.Input.AcceptedScans,Is.EqualTo(1));
            focus.Input.ObserveSource(17,true,true);
            Assert.That(focus.TryScan(17),Is.True);
            Object.Destroy(root); Object.Destroy(pulse);
            yield return null;
        }
    }
}
