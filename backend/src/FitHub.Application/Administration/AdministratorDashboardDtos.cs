namespace FitHub.Application.Administration;
public sealed record TrendPointDto(DateTimeOffset Date,int Count);
public sealed record EquipmentOperationsDto(string Name,decimal UtilizationPercent,decimal DowntimeHours);
public sealed record RecentMemberDto(Guid Id,string Name,string Email,DateTimeOffset RegisteredAt);
public sealed record AdministratorDashboardDto(IReadOnlyDictionary<string,int> UsersByRole,IReadOnlyDictionary<string,int> MembershipsByStatus,decimal CompletedRevenue,string RevenueCurrency,int ActiveMembers,int ActiveMemberships,int PaymentsToday,int PaymentsThisMonth,int PendingClassApprovals,int TrainerCount,IReadOnlyCollection<RecentMemberDto> RecentMembers,IReadOnlyCollection<TrendPointDto> AttendanceTrend,int WorkoutsLast30Days,int MembersWithWorkoutsLast30Days,decimal EquipmentUtilizationPercent,decimal EquipmentDowntimeHours,IReadOnlyCollection<EquipmentOperationsDto> EquipmentOperations,int OpenFaultReports,IReadOnlyDictionary<string,int> ParkingByStatus);
public sealed record CsvReport(string FileName,string Content);
