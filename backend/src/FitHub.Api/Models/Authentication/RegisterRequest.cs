using System.ComponentModel.DataAnnotations;

namespace FitHub.Api.Models.Authentication;

public sealed record RegisterRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(100, MinimumLength = 1)] string FirstName,
    [Required, StringLength(100, MinimumLength = 1)] string LastName,
    [Required, StringLength(256, MinimumLength = 12), RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$",
        ErrorMessage = "Password must include uppercase, lowercase, number, and symbol characters.")] string Password,
    bool ConsentAccepted);
