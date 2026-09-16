using System.ComponentModel.DataAnnotations;

namespace FitHub.Api.Models.Administration;

public sealed record SetActiveRequest(bool Active);
public sealed record SetEnabledRequest(bool Enabled);
public sealed record CreateTrainerRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(100, MinimumLength = 1)] string FirstName,
    [Required, StringLength(100, MinimumLength = 1)] string LastName,
    [Required, StringLength(256, MinimumLength = 12), RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$")]
    string TemporaryPassword);
