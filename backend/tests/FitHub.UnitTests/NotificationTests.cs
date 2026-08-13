using FitHub.Domain.Communications;
namespace FitHub.UnitTests;
public sealed class NotificationTests
{
    [Theory][InlineData(NotificationType.MembershipExpiry)][InlineData(NotificationType.ClassReminder)][InlineData(NotificationType.WorkoutConsistency)][InlineData(NotificationType.StreakWarning)][InlineData(NotificationType.PersonalRecord)][InlineData(NotificationType.FaultReportStatusUpdate)]public void EveryRequiredNotificationTypeCanBeCreated(NotificationType type){var item=new Notification(Guid.NewGuid(),type,"Title","Message",$"key:{type}");Assert.Equal(type,item.Type);Assert.False(item.IsRead);Assert.NotEmpty(item.DeduplicationKey);}
    [Fact]public void NotificationCanBeMarkedReadIdempotently(){var item=new Notification(Guid.NewGuid(),NotificationType.ClassReminder,"Class","Soon","class:1");var at=DateTimeOffset.UtcNow;item.MarkRead(at);item.MarkRead(at.AddMinutes(1));Assert.True(item.IsRead);Assert.Equal(at,item.ReadAt);}
}
