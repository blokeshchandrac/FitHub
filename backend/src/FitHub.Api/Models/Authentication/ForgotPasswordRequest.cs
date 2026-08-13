using System.ComponentModel.DataAnnotations;

namespace FitHub.Api.Models.Authentication;

public sealed record ForgotPasswordRequest(
    [Required, EmailAddress, StringLength(254)] string Email);
