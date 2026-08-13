using FitHub.Domain.Common;
using FitHub.Domain.Identity;

namespace FitHub.Domain.Attendance;

public sealed class Attendance : Entity
{
    private Attendance() { }

    public Attendance(Guid userId, DateTimeOffset checkedInAt)
    {
        UserId = userId;
        CheckedInAt = checkedInAt;
    }

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public DateTimeOffset CheckedInAt { get; set; }
    public DateTimeOffset? CheckedOutAt { get; set; }
}
