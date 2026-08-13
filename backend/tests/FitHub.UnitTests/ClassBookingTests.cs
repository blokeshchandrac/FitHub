using FitHub.Domain.Bookings;
namespace FitHub.UnitTests;
public sealed class ClassBookingTests
{
    private static readonly DateTimeOffset Now=new(2026,8,12,12,0,0,TimeSpan.Zero);
    [Fact]public void ScheduleRequiresPositiveCapacity()=>Assert.Throws<ArgumentOutOfRangeException>(()=>new ClassSchedule("Yoga",null,Now.AddDays(1),Now.AddDays(1).AddHours(1),0,Guid.NewGuid()));
    [Fact]public void ScheduleRequiresValidTimeRange()=>Assert.Throws<ArgumentException>(()=>new ClassSchedule("Yoga",null,Now.AddHours(2),Now.AddHours(1),5,Guid.NewGuid()));
    [Theory][InlineData(BookingStatus.Confirmed)][InlineData(BookingStatus.Waitlisted)]public void NewBookingSupportsConfirmedOrWaitlisted(BookingStatus status){var item=new ClassBooking(Guid.NewGuid(),Guid.NewGuid(),status,Now);Assert.Equal(status,item.Status);}
    [Theory][InlineData(BookingStatus.Cancelled)][InlineData(BookingStatus.Attended)][InlineData(BookingStatus.NoShow)]public void NewBookingRejectsTerminalStatus(BookingStatus status)=>Assert.Throws<ArgumentException>(()=>new ClassBooking(Guid.NewGuid(),Guid.NewGuid(),status,Now));
    [Fact]public void CancelledBookingCanBeRebooked(){var item=new ClassBooking(Guid.NewGuid(),Guid.NewGuid(),BookingStatus.Confirmed,Now);item.Cancel(Now.AddMinutes(1));item.Rebook(BookingStatus.Waitlisted,Now.AddMinutes(2));Assert.Equal(BookingStatus.Waitlisted,item.Status);}
    [Fact]public void ActiveDuplicateCannotBeRebooked(){var item=new ClassBooking(Guid.NewGuid(),Guid.NewGuid(),BookingStatus.Confirmed,Now);Assert.Throws<InvalidOperationException>(()=>item.Rebook(BookingStatus.Confirmed,Now));}
    [Fact]public void WaitlistedBookingCanBePromoted(){var item=new ClassBooking(Guid.NewGuid(),Guid.NewGuid(),BookingStatus.Waitlisted,Now);item.Promote(Now.AddMinutes(1));Assert.Equal(BookingStatus.Confirmed,item.Status);}
}
