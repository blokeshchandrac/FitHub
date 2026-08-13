using FitHub.Domain.Common;
using FitHub.Domain.Identity;

namespace FitHub.Domain.Auditing;

public sealed class AuditLog : Entity
{
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
