namespace FitHub.Application.Bookings;
public sealed record ClassScheduleDto(Guid Id,string Name,string? Description,DateTimeOffset StartsAt,DateTimeOffset EndsAt,int Capacity,int ConfirmedCount,int WaitlistCount,Guid? InstructorUserId,string? MyStatus);
public sealed record CreateClassScheduleCommand(string Name,string? Description,DateTimeOffset StartsAt,DateTimeOffset EndsAt,int Capacity,Guid InstructorUserId);
