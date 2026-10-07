using NUnit.Framework;

namespace FocusCore.Tests
{
    public class FocusInputRouterTests
    {
        private FocusInputRouter Ready()
        {
            var router = new FocusInputRouter();
            router.SetAvailability(true, true, false);
            return router;
        }

        [Test]
        public void HeldSourceNeedsObservedNeutralBeforeFirstScan()
        {
            var router = Ready();
            router.ObserveSource(10, true, false);
            Assert.That(router.TryBeginScan(10), Is.False);
            router.ObserveSource(10, true, true);
            router.ObserveSource(10, true, false);
            Assert.That(router.TryBeginScan(10), Is.True);
            Assert.That(router.AcceptedScans, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateAndBusySecondSourceDoNotQueueAnotherScan()
        {
            var router = Ready();
            router.ObserveSource(10, true, true);
            router.ObserveSource(20, true, true);
            Assert.That(router.TryBeginScan(10), Is.True);
            Assert.That(router.TryBeginScan(10), Is.False);
            Assert.That(router.TryBeginScan(20), Is.False);
            router.Tick(2);
            Assert.That(router.TryBeginScan(20), Is.False);
            router.ObserveSource(20, true, true);
            Assert.That(router.TryBeginScan(20), Is.True);
            Assert.That(router.AcceptedScans, Is.EqualTo(2));
        }

        [Test]
        public void CancellationWhileHeldDoesNotBecomePhysicalRelease()
        {
            var router = Ready();
            router.ObserveSource(10, true, true);
            router.TryBeginScan(10);
            router.CancelSource(10);
            router.Tick(2);
            router.ObserveSource(10, true, false);
            Assert.That(router.TryBeginScan(10), Is.False);
            router.ObserveSource(10, true, true);
            Assert.That(router.TryBeginScan(10), Is.True);
        }

        [Test]
        public void HandLossDoesNotStopScanOrDisableControllerFallback()
        {
            var router = Ready();
            router.ObserveSource(10, true, true);
            router.ObserveSource(30, true, true);
            router.TryBeginScan(10);
            router.ObserveSource(10, false, true);
            Assert.That(router.Mode, Is.EqualTo(FocusMode.Scanning));
            router.Tick(2);
            router.ObserveSource(10, true, false);
            Assert.That(router.TryBeginScan(10), Is.False);
            Assert.That(router.TryBeginScan(30), Is.True);
        }

        [Test]
        public void SuspensionCancelsAndRequiresNeutralAfterRecovery()
        {
            var router = Ready();
            router.ObserveSource(10, true, true);
            router.TryBeginScan(10);
            router.SetAvailability(true, false, false);
            Assert.That(router.Mode, Is.EqualTo(FocusMode.Suspended));
            Assert.That(router.Progress, Is.Zero);
            router.ObserveSource(10, true, true); // Paused input cannot arm recovery.
            router.SetAvailability(true, true, false);
            router.ObserveSource(10, true, false);
            Assert.That(router.Mode, Is.EqualTo(FocusMode.Ready));
            Assert.That(router.TryBeginScan(10), Is.False);
            router.ObserveSource(10, true, true);
            Assert.That(router.TryBeginScan(10), Is.True);
        }

        [Test]
        public void UnknownOrUntrackedPointersAreRejected()
        {
            var router = Ready();
            Assert.That(router.TryBeginScan(999), Is.False);
            router.ObserveSource(10, false, true);
            Assert.That(router.TryBeginScan(10), Is.False);
            Assert.That(router.AcceptedScans, Is.Zero);
        }
    }
}
