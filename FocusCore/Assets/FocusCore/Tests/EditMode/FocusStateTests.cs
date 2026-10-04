using System;
using NUnit.Framework;

namespace FocusCore.Tests
{
    public class FocusStateTests
    {
        private static FocusState Ready()
        {
            var state = new FocusState();
            state.SetAvailability(true, true, false);
            return state;
        }

        [Test]
        public void UnavailableXRRejectsScan()
        {
            var state = new FocusState();
            Assert.That(state.TryStartScan(), Is.False);
            Assert.That(state.Mode, Is.EqualTo(FocusMode.Unavailable));
        }

        [Test]
        public void DuplicateRequestsAreIgnored()
        {
            var state = Ready();
            Assert.That(state.TryStartScan(), Is.True);
            state.Tick(0.5);
            for (var i = 0; i < 10; i++) Assert.That(state.TryStartScan(), Is.False);
            Assert.That(state.Progress, Is.EqualTo(0.4).Within(1e-9));
        }

        [Test]
        public void ScanExpiresAt125Seconds()
        {
            var state = Ready();
            state.TryStartScan();
            state.Tick(1.24);
            Assert.That(state.Mode, Is.EqualTo(FocusMode.Scanning));
            state.Tick(0.01);
            Assert.That(state.Mode, Is.EqualTo(FocusMode.Ready));
            Assert.That(state.TryStartScan(), Is.True);
            Assert.That(state.Progress, Is.EqualTo(0));
        }

        [Test]
        public void LongFrameCompletesScan()
        {
            var state = Ready();
            state.TryStartScan();
            state.Tick(5);
            Assert.That(state.Mode, Is.EqualTo(FocusMode.Ready));
            Assert.That(state.Progress, Is.EqualTo(0));
            Assert.That(state.TryStartScan(), Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void RecoveryWaitsForAllPrerequisites(bool restoreTrackingFirst)
        {
            var state = Ready();
            state.TryStartScan();
            state.SetAvailability(true, false, true);
            state.SetAvailability(true, restoreTrackingFirst, restoreTrackingFirst);
            Assert.That(state.Mode, Is.EqualTo(FocusMode.Suspended));
            Assert.That(state.TryStartScan(), Is.False);
            state.SetAvailability(true, true, false);
            Assert.That(state.Mode, Is.EqualTo(FocusMode.Ready));
            Assert.That(state.Progress, Is.EqualTo(0));
        }

        [Test]
        public void SuspensionCancelsScan()
        {
            var state = Ready();
            state.TryStartScan();
            state.Tick(0.5);
            state.SetAvailability(true, true, true);
            Assert.That(state.Mode, Is.EqualTo(FocusMode.Suspended));
            Assert.That(state.TryStartScan(), Is.False);
            state.SetAvailability(true, true, false);
            Assert.That(state.Mode, Is.EqualTo(FocusMode.Ready));
            Assert.That(state.Progress, Is.EqualTo(0));
        }

        [Test]
        public void RepeatedReadyStatusDoesNotResetScan()
        {
            var state = Ready();
            state.TryStartScan();
            state.Tick(0.5);
            state.SetAvailability(true, true, false);
            Assert.That(state.Mode, Is.EqualTo(FocusMode.Scanning));
            Assert.That(state.Progress, Is.EqualTo(0.4).Within(1e-9));
        }

        [Test]
        public void HeadLossCancelsScanUntilTrackingReturns()
        {
            var state = Ready();
            state.TryStartScan();
            state.SetAvailability(true, false, false);
            Assert.That(state.Mode, Is.EqualTo(FocusMode.Suspended));
            Assert.That(state.TryStartScan(), Is.False);
            state.SetAvailability(true, true, false);
            Assert.That(state.Mode, Is.EqualTo(FocusMode.Ready));
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidDurationIsRejected(double duration)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FocusState(duration));
        }

        [TestCase(-1)]
        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        public void InvalidElapsedTimeCannotCorruptActiveScan(double delta)
        {
            var state = Ready();
            state.TryStartScan();
            state.Tick(0.5);
            Assert.Throws<ArgumentOutOfRangeException>(() => state.Tick(delta));
            Assert.That(state.Mode, Is.EqualTo(FocusMode.Scanning));
            Assert.That(state.Progress, Is.EqualTo(0.4).Within(1e-9));
        }
    }
}
