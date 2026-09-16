namespace FitHub.Application.Bookings;
public interface IClassBookingService
{
    Task<IReadOnlyCollection<ClassScheduleDto>> ListForMemberAsync(Guid userId,CancellationToken cancellationToken=default);
    Task<IReadOnlyCollection<ClassScheduleDto>> ListAsync(Guid userId,CancellationToken cancellationToken=default);
    Task<IReadOnlyCollection<ClassScheduleDto>> ListForTrainerAsync(Guid trainerId,CancellationToken cancellationToken=default);
    Task<IReadOnlyCollection<ClassScheduleDto>> ListForAdministrationAsync(CancellationToken cancellationToken=default);
    Task<ClassScheduleDto> CreateAsync(CreateClassScheduleCommand command,CancellationToken cancellationToken=default);
    Task<ClassScheduleDto?> UpdateAsync(Guid scheduleId,Guid trainerId,CreateClassScheduleCommand command,CancellationToken cancellationToken=default);
    Task<ClassScheduleDto?> ReviewAsync(Guid scheduleId,Guid administratorId,ReviewClassScheduleCommand command,CancellationToken cancellationToken=default);
    Task<IReadOnlyCollection<ClassParticipantDto>?> ListParticipantsAsync(Guid scheduleId,Guid staffUserId,bool administrator,CancellationToken cancellationToken=default);
    Task<ClassScheduleDto?> RecordAttendanceAsync(Guid scheduleId,Guid memberId,Guid staffUserId,bool administrator,bool attended,CancellationToken cancellationToken=default);
    Task<ClassScheduleDto?> BookAsync(Guid scheduleId,Guid userId,CancellationToken cancellationToken=default);
    Task<ClassScheduleDto?> CancelAsync(Guid scheduleId,Guid userId,CancellationToken cancellationToken=default);
}
