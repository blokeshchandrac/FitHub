namespace FitHub.Application.Bookings;

public sealed record ClassScheduleDto(Guid Id,string Name,string? Description,DateTimeOffset StartsAt,DateTimeOffset EndsAt,int Capacity,int ConfirmedCount,int WaitlistCount,Guid? InstructorUserId,string? InstructorName,string? Location,string Status,string? ReviewReason,string? MyStatus);
public sealed record ClassParticipantDto(Guid UserId,string DisplayName,string Email,string BookingStatus,DateTimeOffset BookedAt);
public sealed record CreateClassScheduleCommand(string Name,string? Description,DateTimeOffset StartsAt,DateTimeOffset EndsAt,int Capacity,Guid InstructorUserId,string? Location=null);
public sealed record ReviewClassScheduleCommand(bool Approve,string? Reason);
