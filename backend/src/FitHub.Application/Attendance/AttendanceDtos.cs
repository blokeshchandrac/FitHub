namespace FitHub.Application.Attendance;

public sealed record CheckInTokenDto(string Token, DateTimeOffset ExpiresAt);
public sealed record AttendanceDto(Guid Id, DateTimeOffset CheckedInAt, DateTimeOffset? CheckedOutAt);
public sealed record AttendanceSummaryDto(int VisitsThisWeek, int VisitsThisMonth, int VisitsInCurrentMembership, decimal? CostPerVisit, IReadOnlyCollection<AttendanceDto> History);
public sealed record CheckInResultDto(Guid AttendanceId, Guid UserId, DateTimeOffset CheckedInAt);
