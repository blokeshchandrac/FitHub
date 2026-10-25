using FitHub.Domain.Communications;
namespace FitHub.UnitTests;
public sealed class NotificationTests
{
    [Theory][InlineData(NotificationType.MembershipExpiry)][InlineData(NotificationType.ClassReminder)][InlineData(NotificationType.WorkoutConsistency)][InlineData(NotificationType.StreakWarning)][InlineData(NotificationType.PersonalRecord)][InlineData(NotificationType.FaultReportStatusUpdate)]public void EveryRequiredNotificationTypeCanBeCreated(NotificationType type){var item=new Notification(Guid.NewGuid(),type,"Title","Message",$"key:{type}");Assert.Equal(type,item.Type);Assert.False(item.IsRead);Assert.NotEmpty(item.DeduplicationKey);}
    [Fact]public void NotificationCanBeMarkedReadIdempotently(){var item=new Notification(Guid.NewGuid(),NotificationType.ClassReminder,"Class","Soon","class:1");var at=DateTimeOffset.UtcNow;item.MarkRead(at);item.MarkRead(at.AddMinutes(1));Assert.True(item.IsRead);Assert.Equal(at,item.ReadAt);}
    [Fact]public void NotificationPreferenceDefaultsAreEnabledAndCanBeChanged(){var userId=Guid.NewGuid();var preferences=NotificationPreference.CreateDefaults(userId).ToArray();Assert.Equal(Enum.GetValues<NotificationType>().Length,preferences.Length);Assert.All(preferences,item=>Assert.True(item.Enabled));var changedAt=DateTimeOffset.UtcNow;preferences[0].SetEnabled(false,changedAt);Assert.False(preferences[0].Enabled);Assert.Equal(changedAt,preferences[0].UpdatedAt);}
}
