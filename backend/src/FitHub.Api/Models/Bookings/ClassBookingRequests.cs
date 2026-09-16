namespace FitHub.Api.Models.Bookings;
public sealed record CreateClassScheduleRequest(string Name,string? Description,DateTimeOffset StartsAt,DateTimeOffset EndsAt,int Capacity,string? Location);
public sealed record ReviewClassScheduleRequest(bool Approve,string? Reason);
public sealed record RecordClassAttendanceRequest(bool Attended);
