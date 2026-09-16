using FitHub.Domain.Common;using FitHub.Domain.Identity;
namespace FitHub.Domain.Bookings;
public sealed class ClassBooking:Entity
{
    private ClassBooking(){}
    public ClassBooking(Guid scheduleId,Guid userId,BookingStatus status,DateTimeOffset bookedAt){if(status is not(BookingStatus.Confirmed or BookingStatus.Waitlisted))throw new ArgumentException("New bookings must be confirmed or waitlisted.");ClassScheduleId=scheduleId;UserId=userId;Status=status;BookedAt=bookedAt;}
    public Guid ClassScheduleId{get;private set;}public ClassSchedule ClassSchedule{get;private set;}=null!;public Guid UserId{get;private set;}public ApplicationUser User{get;private set;}=null!;public BookingStatus Status{get;private set;}=BookingStatus.Confirmed;public DateTimeOffset BookedAt{get;private set;}
    public void Cancel(DateTimeOffset at){if(Status==BookingStatus.Cancelled)throw new InvalidOperationException("Booking is already cancelled.");Status=BookingStatus.Cancelled;MarkUpdated(at);}
    public void Rebook(BookingStatus status,DateTimeOffset at){if(Status!=BookingStatus.Cancelled)throw new InvalidOperationException("An active booking already exists.");Status=status;BookedAt=at;MarkUpdated(at);}
    public void Promote(DateTimeOffset at){if(Status!=BookingStatus.Waitlisted)throw new InvalidOperationException("Only a waitlisted booking can be promoted.");Status=BookingStatus.Confirmed;MarkUpdated(at);}
    public void RecordAttendance(bool attended,DateTimeOffset at){if(Status!=BookingStatus.Confirmed)throw new InvalidOperationException("Only a confirmed booking can receive attendance.");Status=attended?BookingStatus.Attended:BookingStatus.NoShow;MarkUpdated(at);}
}
