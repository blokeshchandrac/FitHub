namespace FitHub.Application.Attendance;

public interface IAttendanceService
{
    Task<CheckInTokenDto> GenerateTokenAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CheckInResultDto> CheckInAsync(string rawToken, CancellationToken cancellationToken = default);
    Task<AttendanceSummaryDto> GetSummaryAsync(Guid userId, CancellationToken cancellationToken = default);
}
