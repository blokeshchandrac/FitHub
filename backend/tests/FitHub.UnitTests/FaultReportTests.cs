using FitHub.Domain.Equipment;
namespace FitHub.UnitTests;
public sealed class FaultReportTests
{
    [Fact] public void OtherRequiresComment() => Assert.Throws<ArgumentException>(() => new FaultReport(Guid.NewGuid(), Guid.NewGuid(), FaultCategory.Other, null, false));
    [Fact] public void OtherWithCommentIsValid() => Assert.Equal("Explain issue", new FaultReport(Guid.NewGuid(), Guid.NewGuid(), FaultCategory.Other, "Explain issue", false).Comment);
    [Fact] public void SeriousFlagRequiresSafetyCategory() => Assert.Throws<ArgumentException>(() => new FaultReport(Guid.NewGuid(), Guid.NewGuid(), FaultCategory.Cleanliness, "Spill", true));
    [Fact] public void AdminStatusRequiresNotes() { var report = new FaultReport(Guid.NewGuid(), Guid.NewGuid(), FaultCategory.BrokenPart, null, false); Assert.Throws<ArgumentException>(() => report.UpdateStatus(FaultReportStatus.UnderReview, "", DateTimeOffset.UtcNow)); }
    [Fact] public void ResolvedStatusRecordsResolutionDate() { var report = new FaultReport(Guid.NewGuid(), Guid.NewGuid(), FaultCategory.BrokenPart, null, false); report.UpdateStatus(FaultReportStatus.Resolved, "Replaced part", DateTimeOffset.UtcNow); Assert.NotNull(report.ResolutionDate); }
}
