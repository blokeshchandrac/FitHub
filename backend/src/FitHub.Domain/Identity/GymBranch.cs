using FitHub.Domain.Common;

namespace FitHub.Domain.Identity;

public sealed class GymBranch : Entity
{
    private GymBranch() { }

    public GymBranch(string code, string name, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length > 20)
            throw new ArgumentException("Branch code must contain between 1 and 20 characters.", nameof(code));
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 120)
            throw new ArgumentException("Branch name must contain between 1 and 120 characters.", nameof(name));

        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        IsActive = isActive;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
}
