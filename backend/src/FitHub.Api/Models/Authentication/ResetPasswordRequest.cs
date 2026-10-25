using System.ComponentModel.DataAnnotations;

namespace FitHub.Api.Models.Authentication;

public sealed record ResetPasswordRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(256, MinimumLength = 32)] string Token,
    [Required, StringLength(256, MinimumLength = 12), RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$",
        ErrorMessage = "Password must include uppercase, lowercase, number, and symbol characters.")] string NewPassword,
    [Required, StringLength(256)] string ConfirmPassword);
