using System.ComponentModel.DataAnnotations;

namespace EnterpriseDashboard.Api.DTOs.Auth;

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(6)] string Password
);
