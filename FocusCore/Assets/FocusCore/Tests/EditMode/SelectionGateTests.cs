using NUnit.Framework;

namespace FocusCore.Tests
{
    public class SelectionGateTests
    {
        [Test]
        public void HeldSelectionDoesNotRepeat()
        {
            var gate = new SelectionGate();
            Assert.That(gate.TrySelect(1), Is.True);
            Assert.That(gate.TrySelect(1), Is.False);
            gate.Release(1);
            Assert.That(gate.TrySelect(1), Is.True);
        }

        [Test]
        public void DifferentSourcesDoNotReleaseEachOther()
        {
            var gate = new SelectionGate();
            Assert.That(gate.TrySelect(1), Is.True);
            Assert.That(gate.TrySelect(2), Is.True);
            gate.Release(1);
            Assert.That(gate.TrySelect(2), Is.False);
            Assert.That(gate.TrySelect(1), Is.True);
        }

        [Test]
        public void TrackingRecoveryRequiresObservedNeutral()
        {
            var gate = new SelectionGate();
            gate.TrySelect(1);
            gate.RequireNeutral(1);
            Assert.That(gate.TrySelect(1), Is.False);
            Assert.That(gate.TrySelect(1), Is.False);
            gate.Release(1); // Adapter may call this only after valid tracked neutral.
            Assert.That(gate.TrySelect(1), Is.True);
        }

        [Test]
        public void RepeatedCancellationCannotRearmSource()
        {
            var gate = new SelectionGate();
            gate.RequireNeutral(1);
            gate.RequireNeutral(1);
            Assert.That(gate.TrySelect(1), Is.False);
        }

        [Test]
        public void LostSourceDoesNotBlockControllerFallbackSource()
        {
            var gate = new SelectionGate();
            gate.RequireNeutral(1);
            Assert.That(gate.TrySelect(3), Is.True);
            Assert.That(gate.TrySelect(1), Is.False);
        }

        [Test]
        public void ScanExpiryDoesNotRearmHeldSelection()
        {
            var gate = new SelectionGate();
            var state = new FocusState();
            state.SetAvailability(true, true, false);
            Assert.That(gate.TrySelect(1) && state.TryStartScan(), Is.True);
            state.Tick(1.25);
            Assert.That(gate.TrySelect(1), Is.False);
            gate.Release(1);
            Assert.That(gate.TrySelect(1) && state.TryStartScan(), Is.True);
        }

        [Test]
        public void HandLossDoesNotCancelAcceptedScan()
        {
            var gate = new SelectionGate();
            var state = new FocusState();
            state.SetAvailability(true, true, false);
            Assert.That(gate.TrySelect(1) && state.TryStartScan(), Is.True);
            gate.RequireNeutral(1);
            state.Tick(1.25);
            Assert.That(state.Mode, Is.EqualTo(FocusMode.Ready));
            Assert.That(gate.TrySelect(1), Is.False);
        }

        [Test]
        public void ClearingDisposedRigAdmitsNewSources()
        {
            var gate = new SelectionGate();
            gate.TrySelect(1);
            gate.RequireNeutral(2);
            gate.Clear();
            Assert.That(gate.TrySelect(1), Is.True);
            Assert.That(gate.TrySelect(2), Is.True);
        }

        [TestCase(0)]
        [TestCase(-25)]
        public void SourceIdentifiersAreOpaqueIntegers(int sourceId)
        {
            var gate = new SelectionGate();
            Assert.That(gate.TrySelect(sourceId), Is.True);
            Assert.That(gate.TrySelect(sourceId), Is.False);
        }
    }
}
