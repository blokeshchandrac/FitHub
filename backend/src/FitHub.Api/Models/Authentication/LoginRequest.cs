using System.ComponentModel.DataAnnotations;

namespace FitHub.Api.Models.Authentication;

public sealed record LoginRequest(
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, StringLength(256, MinimumLength = 1)] string Password);
