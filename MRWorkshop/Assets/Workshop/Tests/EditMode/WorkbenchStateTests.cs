using NUnit.Framework;
namespace MRWorkshop.Tests
{
    public sealed class WorkbenchStateTests
    {
        [Test] public void CompatibleReleasedCellWithinRadiusDocks()
        { var state=new WorkbenchState(); Assert.That(state.TryDock("cell",PartKind.PowerCell,false,.14f),Is.True); Assert.That(state.DockedId,Is.EqualTo("cell")); }
        [Test] public void HeldAndIncompatiblePartsReject()
        { var s=new WorkbenchState(); Assert.That(s.TryDock("a",PartKind.PowerCell,true,0),Is.False); Assert.That(s.TryDock("b",PartKind.Cube,false,0),Is.False); Assert.That(s.DockedId,Is.Null); }
        [Test] public void OccupiedSocketDoesNotReplaceOrDuplicate()
        { var s=new WorkbenchState(); s.TryDock("a",PartKind.PowerCell,false,0); Assert.That(s.TryDock("b",PartKind.PowerCell,false,0),Is.False); Assert.That(s.TryDock("a",PartKind.PowerCell,false,0),Is.False); Assert.That(s.DockedId,Is.EqualTo("a")); }
        [TestCase(.141f)] [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidOrFarDistanceRejects(float distance)
        { Assert.That(new WorkbenchState().TryDock("a",PartKind.PowerCell,false,distance),Is.False); }
        [TestCase(null)] [TestCase("")] public void EmptyIdsReject(string id)
        { Assert.That(new WorkbenchState().TryDock(id,PartKind.PowerCell,false,0),Is.False); }
        [Test] public void UndockingWrongIdDoesNotLoseOwnership()
        { var s=new WorkbenchState(); s.TryDock("a",PartKind.PowerCell,false,0); Assert.That(s.Undock("b"),Is.False); Assert.That(s.DockedId,Is.EqualTo("a")); Assert.That(s.Undock("a"),Is.True); Assert.That(s.DockedId,Is.Null); }
        [Test] public void PowerNeedsCellAndStopsOnRemoval()
        { var s=new WorkbenchState(); s.TogglePower(); Assert.That(s.Running,Is.False); s.TryDock("a",PartKind.PowerCell,false,0); Assert.That(s.Running,Is.True); s.Undock("a"); Assert.That(s.Running,Is.False); }
        [Test] public void ResetClearsPowerAndOwnership()
        { var s=new WorkbenchState(); s.TryDock("a",PartKind.PowerCell,false,0); s.TogglePower(); s.Reset(); Assert.That(s.PowerRequested,Is.False); Assert.That(s.DockedId,Is.Null); Assert.That(s.Running,Is.False); }
        [TestCase(.1f,.75f)] [TestCase(1.1f,1.1f)] [TestCase(3f,1.5f)] [TestCase(float.NaN,1f)]
        public void SizeIsBoundedAndFinite(float requested,float expected)
        { Assert.That(WorkbenchState.ClampSize(requested),Is.EqualTo(expected)); }
    }
}
