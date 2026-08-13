namespace FitHub.Application.Administration;
public sealed record TrendPointDto(DateTimeOffset Date,int Count);
public sealed record EquipmentOperationsDto(string Name,decimal UtilizationPercent,decimal DowntimeHours);
public sealed record AdministratorDashboardDto(IReadOnlyDictionary<string,int> UsersByRole,IReadOnlyDictionary<string,int> MembershipsByStatus,decimal CompletedRevenue,string RevenueCurrency,IReadOnlyCollection<TrendPointDto> AttendanceTrend,int WorkoutsLast30Days,int MembersWithWorkoutsLast30Days,decimal EquipmentUtilizationPercent,decimal EquipmentDowntimeHours,IReadOnlyCollection<EquipmentOperationsDto> EquipmentOperations,int OpenFaultReports,IReadOnlyDictionary<string,int> ParkingByStatus);
public sealed record CsvReport(string FileName,string Content);
