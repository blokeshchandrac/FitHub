namespace FitHub.Api.Models.Bookings;
public sealed record CreateClassScheduleRequest(string Name,string? Description,DateTimeOffset StartsAt,DateTimeOffset EndsAt,int Capacity,Guid? InstructorUserId);
